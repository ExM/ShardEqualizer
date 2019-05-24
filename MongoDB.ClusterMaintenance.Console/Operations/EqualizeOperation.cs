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
			
			var shards = await _configDb.Shards.GetAll();
			var collStatsMap = (await listCollStats(token)).ToDictionary(_ => _.Ns);

			var unShardedSizeMap = collStatsMap.Values
				.Where(_ => !_.Sharded)
				.GroupBy(_ => _.Primary)
				.ToDictionary(k => k.Key, g => g.Sum(_ => _.Size));

			var shardByTag =  _intervals
				.SelectMany(_ => _.Zones)
				.Distinct()
				.ToDictionary(_ => _, _ => shards.Single(s => s.Tags.Contains(_)));

			var collSizeSumByShard = shards.Select(_ => _.Id).ToDictionary(
				shId => shId,
				shId => _intervals
					.Where(i => i.Zones.Select(t => shardByTag[t].Id).Contains(shId))
					.Select(i => collStatsMap[i.Namespace].Size)
					.Sum());

			foreach (var interval in _intervals.Where(_ => _.Selected).Where(_ => _.Correction != CorrectionMode.None))
			{
				var collSize = collStatsMap[interval.Namespace].Size;

				Dictionary<TagIdentity, long> correctionSize = null;

				if (interval.Correction == CorrectionMode.UnShard)
				{
					correctionSize = new Dictionary<TagIdentity, long>();
					foreach (var tag in interval.Zones)
					{
						var shId = shardByTag[tag].Id;
						var shSize = unShardedSizeMap[shId];
						var correctionPart = (double) collSize / collSizeSumByShard[shId];
						correctionSize[tag] = (long) Math.Truncate(shSize * correctionPart);
					}
				}

				_log.Info("Equalize shards from {0}", interval.Namespace.FullName);
				_commandPlanWriter.Comment($"Equalize shards from {interval.Namespace.FullName}");
				await equalizeShards(interval, targetDeviation, collStatsMap[interval.Namespace], shards, correctionSize, token);
			}
		}

		private async Task<IReadOnlyList<CollStatsResult>> listCollStats(CancellationToken token)
		{
			_log.Info("list coll stats...");
			var allCollectionNames = await _mongoClient.ListUserCollections(token);
			_log.Info("Found: {0} collections", allCollectionNames.Count);

			return await allCollectionNames.ParallelsAsync(runCollStats, 32, token);
		}
		
		private async Task<CollStatsResult> runCollStats(CollectionNamespace ns, CancellationToken token)
		{
			_log.Info("collection: {0}", ns);
			var db = _mongoClient.GetDatabase(ns.DatabaseNamespace.DatabaseName);
			return await db.CollStats(ns.CollectionName, 1, token);
		}

		private async Task equalizeShards(Interval interval, long targetDeviation, CollStatsResult collStats, IReadOnlyCollection<Shard> shards, IDictionary<TagIdentity, long> sizeCorrection, CancellationToken token)
		{
			var tagRanges = await _configDb.Tags.Get(interval.Namespace, interval.Min, interval.Max);

			if (tagRanges.Count == 0)
			{
				_log.Info("tag ranges not found");
				_commandPlanWriter.Comment("no tag ranges");
				_commandPlanWriter.Comment("---");
				return;
			}
			
			var allChunks = await (await _configDb.Chunks
				.ByNamespace(interval.Namespace)
				.From(interval.Min)
				.To(interval.Max)
				.Find()).ToListAsync(token);
			
			var collInfo = await _configDb.Collections.Find(interval.Namespace);
			var db = _mongoClient.GetDatabase(interval.Namespace.DatabaseNamespace.DatabaseName);
			
			var chunkColl = new ChunkCollection(allChunks);

			async Task<long> chunkSizeResolver(string chunkId)
			{
				var chunk = chunkColl.ById(chunkId);
				var result = await db.Datasize(collInfo, chunk, token);
				return result.Size;
			}

			var equalizer = new ShardSizeEqualizer(shards, collStats.Shards, tagRanges, chunkColl, chunkSizeResolver);
			if(sizeCorrection != null)
				foreach (var pair in sizeCorrection)
				{
					equalizer.Zones.Single(_ => _.Tag == pair.Key).Correction(pair.Value);
				}

			foreach (var zone in equalizer.Zones)
			{
				_log.Info("Zone: {0} Coll: {1} UnShardCorrection: {2}",
					zone.Tag, zone.InitialSize.ByteSize(), zone.UnShardCorrection.ByteSize());
			}
			
			var rounds = 0;
			var progress = new TargetProgressReporter(equalizer.CurrentSizeDeviation, targetDeviation, LongExtensions.ByteSize, () =>
			{
				_log.Info("Rounds: {0} State: {1}", rounds, equalizer.RenderState());
			});
			
			while(equalizer.CurrentSizeDeviation >= targetDeviation)
			{
				if (!await equalizer.Equalize())
					break;

				rounds++;
				progress.Update(equalizer.CurrentSizeDeviation);
				token.ThrowIfCancellationRequested();
			}

			progress.Update(equalizer.CurrentSizeDeviation);
			await progress.Finalize();

			if (rounds == 0)
			{
				_commandPlanWriter.Comment("no correction");
				_commandPlanWriter.Comment("---");
				return;
			}
			
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