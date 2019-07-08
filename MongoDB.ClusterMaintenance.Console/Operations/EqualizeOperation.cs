using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
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
		private long? _moveLimit;
		private readonly bool _planOnly;

		public EqualizeOperation(IConfigDbRepositoryProvider configDb, IReadOnlyList<Interval> intervals,
			IMongoClient mongoClient, CommandPlanWriter commandPlanWriter, long? moveLimit, bool planOnly)
		{
			_configDb = configDb;
			_intervals = intervals;
			_mongoClient = mongoClient;
			_commandPlanWriter = commandPlanWriter;
			_moveLimit = moveLimit;
			_planOnly = planOnly;
		}

		public async Task Run(CancellationToken token)
		{
			var chunkSize = await _configDb.Settings.GetChunksize();
			var shards = await _configDb.Shards.GetAll();
			var collStatsMap = (await listCollStats(token)).ToDictionary(_ => _.Ns);
			
			var chunksByCollection = await loadAllCollChunks(token);

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
					.Where(i => i.Correction == CorrectionMode.UnShard && i.Zones.Select(t => shardByTag[t].Id).Contains(shId))
					.Select(i => collStatsMap[i.Namespace].Size)
					.Sum());

			var shardSize = shards.ToDictionary(_ => _.Id, _ => (long) 0);
			foreach (var collStats in collStatsMap.Values)
			{
				if (collStats.Sharded)
				{
					foreach (var pair in collStats.Shards)
						shardSize[pair.Key] += pair.Value.Size;
				}
				else
				{
					shardSize[collStats.Primary] += collStats.Size;
				}
			}

			var shardAvgSize = shardSize.Values.Sum() / shardSize.Count;
			
			var shardSizeCorrection = shardSize.ToDictionary(_ => _.Key, _ => shardAvgSize - _.Value);
			
			var headMsg = "";
			
			foreach (var shard in shards)
				headMsg += ";" + shard.Id;
			
			Console.WriteLine(headMsg);
			
			var sizeMsg = "";
			
			foreach (var shard in shards)
				sizeMsg += ";" + shardSize[shard.Id] /1024/1024;
			
			Console.WriteLine(sizeMsg);
			
			var deltaMsg = "";
			
			foreach (var shard in shards)
				deltaMsg += ";" + shardSizeCorrection[shard.Id] /1024/1024;
			
			Console.WriteLine(deltaMsg);
			
			var zoneOpt = new ZoneOptimizationDescriptor(_intervals.Where(_ => _.Correction != CorrectionMode.None).Select(_=> _.Namespace), shards.Select(_ => _.Id));

			foreach (var p in unShardedSizeMap)
				zoneOpt.UnShardedSize[p.Key] = p.Value;

			foreach (var coll in zoneOpt.Collections)
			{
				if(!collStatsMap[coll].Sharded)
					continue;

				foreach (var s in collStatsMap[coll].Shards)
					zoneOpt[coll, s.Key].CurrentSize = s.Value.Size;
			}

			foreach (var interval in _intervals.Where(_ => _.Correction != CorrectionMode.None))
			{
				var allChunks = chunksByCollection[interval.Namespace];
				foreach (var tag in interval.Zones)
				{
					var shard = shardByTag[tag].Id;

					var bucket = zoneOpt[interval.Namespace, shard];
					
					bucket.Managed = true;

					var movedChunks = allChunks.Count(_ => _.Shard == shard && !_.Jumbo);
					if (movedChunks <= 1)
					{
						bucket.MinSize = bucket.CurrentSize;
						_log.Info("Disable size reduction {0} on {1}", interval.Namespace, shard);
					}
					else
						bucket.MinSize = bucket.CurrentSize - chunkSize * (movedChunks - 1);
				}
			}

			var solver = zoneOpt.BuildSolver();

			if(!solver.Find())
				throw new Exception("solution for zone optimization not found");
			
			_log.Info("Found solution with max deviation {0} by shards", zoneOpt.TargetShardMaxDeviation.ByteSize());
			_commandPlanWriter.Comment($"Found solution with max deviation {zoneOpt.TargetShardMaxDeviation.ByteSize()} by shards");

			foreach(var msg in solver.ActiveConstraints)
				_log.Info("Active constraint: {0}", msg);
			
			foreach (var interval in _intervals.Where(_ => _.Selected).Where(_ => _.Correction != CorrectionMode.None))
			{
				var targetSize = new Dictionary<TagIdentity, long>();

				if (interval.Correction == CorrectionMode.UnShard)
				{
					foreach (var tag in interval.Zones)
					{
						var shId = shardByTag[tag].Id;
						targetSize[tag] = zoneOpt[interval.Namespace, shId].TargetSize;
					}
				}
				else
				{
					var collStat = collStatsMap[interval.Namespace];
					var managedCollSize = interval.Zones.Sum(tag => collStat.Shards[shardByTag[tag].Id].Size);
					var avgZoneSize = managedCollSize / interval.Zones.Count;

					foreach (var tag in interval.Zones)
						targetSize[tag] = avgZoneSize;
				}

				_log.Info("Equalize shards from {0}", interval.Namespace.FullName);
				_commandPlanWriter.Comment($"Equalize shards from {interval.Namespace.FullName}");
				await equalizeShards(interval, collStatsMap[interval.Namespace], shards, targetSize, chunksByCollection[interval.Namespace], token);
			}
		}
		
		private async Task<IReadOnlyDictionary<CollectionNamespace, List<Chunk>>> loadAllCollChunks(CancellationToken token)
		{
			_log.Info("load coll chunks...");

			return (await _intervals
				.Where(_ => _.Correction != CorrectionMode.None)
				.ToList()
				.ParallelsAsync(loadCollChunks, 32, token)).ToDictionary(_ => _.Item1, _ => _.Item2);
		}
		
		private async Task<Tuple<CollectionNamespace, List<Chunk>>> loadCollChunks(Interval interval, CancellationToken token)
		{
			_log.Info("load chunks collection: {0}", interval.Namespace);
			var allChunks = await (await _configDb.Chunks
				.ByNamespace(interval.Namespace)
				.From(interval.Min)
				.To(interval.Max)
				.Find()).ToListAsync(token);
			return new Tuple<CollectionNamespace, List<Chunk>>(interval.Namespace, allChunks);
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

		private async Task equalizeShards(Interval interval, CollStatsResult collStats,
			IReadOnlyCollection<Shard> shards, IDictionary<TagIdentity, long> targetSize, List<Chunk> allChunks,
			CancellationToken token)
		{
			var tagRanges = await _configDb.Tags.Get(interval.Namespace, interval.Min, interval.Max);

			if (tagRanges.Count == 0)
			{
				_log.Info("tag ranges not found");
				_commandPlanWriter.Comment("no tag ranges");
				_commandPlanWriter.Comment("---");
				return;
			}
			
			var collInfo = await _configDb.Collections.Find(interval.Namespace);
			var db = _mongoClient.GetDatabase(interval.Namespace.DatabaseNamespace.DatabaseName);
			
			async Task<long> chunkSizeResolver(Chunk chunk)
			{
				var result = await db.Datasize(collInfo, chunk, token);
				return result.Size;
			}
			
			var chunkColl = new ChunkCollection(allChunks, chunkSizeResolver);
			var equalizer = new ShardSizeEqualizer(shards, collStats.Shards, tagRanges, targetSize, chunkColl, _moveLimit);

			var lastZone = equalizer.Zones.Last();
			foreach (var zone in equalizer.Zones)
			{
				_log.Info("Zone: {0} Coll: {1} -> {2}",
					zone.Tag, zone.InitialSize.ByteSize(), zone.TargetSize.ByteSize());
				if(zone != lastZone)
					_log.Info("RequireShiftSize: {0} ", zone.Right.RequireShiftSize.ByteSize());
			}
			
			if (_planOnly)
			{
				return;
			}
			
			var rounds = 0;
			var progress = new TargetProgressReporter(equalizer.MovedSize, equalizer.RequireMoveSize, LongExtensions.ByteSize, () =>
			{
				_log.Info("Rounds: {0} SizeDeviation: {1}", rounds, equalizer.CurrentSizeDeviation.ByteSize());
				_log.Info(equalizer.RenderState());
			});
			
			while(await equalizer.Equalize())
			{
				rounds++;
				progress.Update(equalizer.MovedSize);
				token.ThrowIfCancellationRequested();
			}

			await progress.Finalize();
			
			if (rounds == 0)
			{
				_commandPlanWriter.Comment("no correction");
				_commandPlanWriter.Comment("---");
				return;
			}
			
			foreach (var zone in equalizer.Zones)
			{
				_log.Info("Zone: {0} InitialSize: {1} CurrentSize: {2} TargetSize: {3}",
					zone.Tag, zone.InitialSize.ByteSize(), zone.CurrentSize.ByteSize(), zone.TargetSize.ByteSize());
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
			_commandPlanWriter.Flush();
		}
	}
}