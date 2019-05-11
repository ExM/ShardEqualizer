using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.ClusterMaintenance.Config;
using MongoDB.ClusterMaintenance.Models;
using MongoDB.ClusterMaintenance.MongoCommands;
using MongoDB.ClusterMaintenance.ShardSizeEqualizing;
using MongoDB.Driver;
using NLog;

namespace MongoDB.ClusterMaintenance.Operations
{
	public class EqualizeOperation: IOperation
	{
		private static readonly Logger _log = LogManager.GetCurrentClassLogger();
			
		private readonly IReadOnlyList<Interval> _intervals;
		private readonly IConfigDbRepositoryProvider _configDb;
		private readonly IMongoClient _mongoClient;
		private readonly CommandPlanWriter _commandPlanWriter;

		public EqualizeOperation(IConfigDbRepositoryProvider configDb, IReadOnlyList<Interval> intervals, IMongoClient mongoClient, CommandPlanWriter commandPlanWriter)
		{
			_configDb = configDb;
			_intervals = intervals;
			_mongoClient = mongoClient;
			_commandPlanWriter = commandPlanWriter;
		}

		public async Task Run(CancellationToken token)
		{
			var chunkSize = await _configDb.Settings.GetChunksize();
			
			var targetDeviation = chunkSize + chunkSize / 64; // < 102%
			
			foreach (var interval in _intervals)
			{
				_log.Info("Equalize shards from {0}", interval.Namespace.FullName);
				_commandPlanWriter.Comment($"Equalize shards from {interval.Namespace.FullName}");
				await equalizeShards(interval, targetDeviation, token);
			}
		}

		private async Task equalizeShards(Interval interval, long targetDeviation, CancellationToken token)
		{
			var shards = await _configDb.Shards.GetAll();
			
			var allChunks = await (await _configDb.Chunks
				.ByNamespace(interval.Namespace)
				.From(interval.Min)
				.To(interval.Max)
				.Find()).ToListAsync(token);
			
			var collInfo = await _configDb.Collections.Find(interval.Namespace);
			var db = _mongoClient.GetDatabase(interval.Namespace.DatabaseNamespace.DatabaseName);

			var collStats = await db.CollStats(interval.Namespace.CollectionName, 1, token);
			
			var chunkColl = new ChunkCollection(allChunks);

			var tagRanges = await _configDb.Tags.Get(interval.Namespace);
			if(interval.Min.HasValue && interval.Max.HasValue)
				tagRanges = tagRanges.Where(r => interval.Min.Value <= r.Min && r.Max <= interval.Max.Value).ToList().AsReadOnly();

			async Task<long> chunkSizeResolver(string chunkId)
			{
				var chunk = chunkColl.ById(chunkId);
				var result = await db.Datasize(collInfo, chunk, token);
				return result.Size;
			}

			var equalizer = new ShardSizeEqualizer(shards, collStats.Shards, tagRanges, chunkColl, chunkSizeResolver);
			var rounds = 0;
			var progress = new TargetProgressReporter(equalizer.CurrentSizeDeviation, targetDeviation, LongExtensions.ByteSize, () =>
			{
				_log.Info("State: {0}", equalizer.RenderState());
				_log.Info("Rounds: {0}", rounds);
			});
			
			while(await equalizer.Equalize())
			{
				if (equalizer.CurrentSizeDeviation < targetDeviation)
					break;
				
				Interlocked.Increment(ref rounds);
				progress.Update(equalizer.CurrentSizeDeviation);
				token.ThrowIfCancellationRequested();
				
				rounds++;
			}

			progress.Update(equalizer.CurrentSizeDeviation);
			await progress.Finalize();
			
			_commandPlanWriter.Comment(equalizer.RenderState());
			_commandPlanWriter.Comment("remove old tags");
			foreach (var tagRange in tagRanges)
			{
				_commandPlanWriter.RemoveTagRange(
					interval.Namespace, tagRange.Min, tagRange.Max, tagRange.Tag);
			}
			
			_commandPlanWriter.Comment("set new tags");
			foreach (var zone in equalizer.Zones)
			{
				_commandPlanWriter.AddTagRange(
					interval.Namespace, zone.Min, zone.Max, zone.Tag);
			}
			
			_commandPlanWriter.Comment("---");
		}
	}
}