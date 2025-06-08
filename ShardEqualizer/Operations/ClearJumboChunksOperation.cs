using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;
using ShardEqualizer.ConfigServices;
using ShardEqualizer.Contracts.UI;
using ShardEqualizer.DAL.Models;
using ShardEqualizer.DAL.Repositories;
using ShardEqualizer.ScriptGen;
using ShardEqualizer.ShardedClusterViews;

namespace ShardEqualizer.Operations;

public class ClearJumboChunksOperation : IOperation
{
	private readonly IReadOnlyList<Interval> _intervals;
	private readonly IProgressCollector _progressRenderer;
	private readonly ShardedCollectionInfoView _shardedCollectionInfoView;
	private readonly ChunkRepository _chunkRepo;
	private readonly CommandPlanWriter _commandPlanWriter;

	public ClearJumboChunksOperation(
		ShardedCollectionInfoView shardedCollectionInfoView,
		ChunkRepository chunkRepo, //TODO use ChunkView
		IReadOnlyList<Interval> intervals,
		IProgressCollector progressRenderer,
		CommandPlanWriter commandPlanWriter)
	{
		_shardedCollectionInfoView = shardedCollectionInfoView;
		_chunkRepo = chunkRepo;
		_commandPlanWriter = commandPlanWriter;

		if (intervals.Count == 0)
			throw new ArgumentException("interval list is empty");

		_intervals = intervals;
		_progressRenderer = progressRenderer;
	}
		
	private async Task<(CollectionNamespace, IList<BoundInterval>)> GetJumboChunks(CollectionNamespace ns, IProgressReporter reporter, CancellationToken token)
	{
		var collectionInfo = await _shardedCollectionInfoView.Get(ns, token);
		var bounds = await (await _chunkRepo.ByUuid(collectionInfo.Uuid).OnlyJumbo().Find(token))
			.ToListAsync(token);
			
		reporter.Increment();

		return (ns, bounds.Select(c => new BoundInterval(){ Min = c.Min, Max = c.Max}).ToList());
	}
		
	private async Task<IDictionary<CollectionNamespace, IEnumerable<BoundInterval>>> GetAllJumboChunks(List<CollectionNamespace> nss, CancellationToken token)
	{
		var allJumboChunks = new Dictionary<CollectionNamespace, IEnumerable<BoundInterval>>();

		await using var reporter = _progressRenderer.Start($"Read jumbo chunks", nss.Count);
			
		var results = await nss.ParallelsAsync((ns, t) => GetJumboChunks(ns, reporter, t), 16, token);

		var allJumboChunkCount = 0;
		foreach (var (ns, bounds) in results)
		{
			if (bounds.Count == 0)
				continue;
				
			allJumboChunks.Add(ns, bounds);
			allJumboChunkCount += bounds.Count;
		}

		reporter.SetCompleteMessage(allJumboChunkCount == 0
			? "No chunks to clear."
			: $"Found {allJumboChunkCount} jumbo chunks.");

		return allJumboChunks;
	}

	private void WriteCommandFile(IDictionary<CollectionNamespace, IEnumerable<BoundInterval>> jumboChunks)
	{
		foreach (var (ns, boundIntervals) in jumboChunks)
		{
			_commandPlanWriter.Comment($"clear jumbo flag on {ns}");

			foreach (var boundInterval in boundIntervals)
			{
				_commandPlanWriter.ClearJumboFlag(ns, boundInterval.Min, boundInterval.Max);
			}

			_commandPlanWriter.Comment(" --");
		}
	}

	public async Task Run(CancellationToken token)
	{
		var nss = _intervals.Select(i => i.Namespace).ToList();

		var jumboChunks = await GetAllJumboChunks(nss, token);

		WriteCommandFile(jumboChunks);
	}

	private class BoundInterval
	{
		public required BsonBound Min { get; init; }
		public required BsonBound Max { get; init; }
	}
}