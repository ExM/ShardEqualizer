using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.ClusterMaintenance.Config;
using MongoDB.ClusterMaintenance.Models;
using MongoDB.ClusterMaintenance.MongoCommands;
using MongoDB.Driver;
using NLog;
using NLog.Fluent;

namespace MongoDB.ClusterMaintenance.Operations
{
	public class FindNewCollectionsOperation: IOperation
	{
		private static readonly Logger _log = LogManager.GetCurrentClassLogger();
		
		private readonly IConfigDbRepositoryProvider _configDb;
		private readonly IMongoClient _mongoClient;
		private readonly IReadOnlyList<Interval> _intervals;
		private readonly JsonWriterSettings _jsonWriterSettings = new JsonWriterSettings()
			{Indent = false, GuidRepresentation = GuidRepresentation.Unspecified, OutputMode = JsonOutputMode.Shell};

		public FindNewCollectionsOperation(IConfigDbRepositoryProvider configDb, IMongoClient mongoClient, IReadOnlyList<Interval> intervals)
		{
			_intervals = intervals;
			_configDb = configDb;
			_mongoClient = mongoClient;
		}
	
		public async Task Run(CancellationToken token)
		{
			var allShards = (await _configDb.Shards.GetAll())
				.Select(_ => _.Id.ToString())
				.OrderBy(_ => _)
				.ToList();
			
			var defaultZones = string.Join(",", allShards);

			var shardedCollections = (await _configDb.Collections.FindAll()).ToDictionary(_ => _.Id);
			
			_log.Info("scan current intervals...");
			foreach (var ns in _intervals.Select(_ => _.Namespace).Distinct())
			{
				if (shardedCollections.TryGetValue(ns, out var shardedCollection))
				{
					if(shardedCollection.Dropped)
						_log.Warn("collection '{0}' dropped", ns);
					
					shardedCollections.Remove(ns);
				}
				else
				{
					_log.Warn("collection '{0}' not sharded", ns);
				}
			}

			if (shardedCollections.Count == 0)
			{
				_log.Info("new sharded collections not found");
				return;
			}

			_log.Info("scan new sharded collections...");
			
			var collTasks = new List<Task<NewShardedCollection>>(shardedCollections.Count);
			var throttler = new SemaphoreSlim(20);

			async Task<NewShardedCollection> runCollStat(ShardedCollectionInfo shardedCollection)
			{
				try
				{
					var ns = shardedCollection.Id;
					_log.Info("collStats '{0}'", ns);
					var db = _mongoClient.GetDatabase(ns.DatabaseNamespace.DatabaseName);
					var collStats = await db.CollStats(ns.CollectionName, 1, token);

					return new NewShardedCollection()
					{
						Info = shardedCollection,
						Stats = collStats
					};
				}
				finally
				{
					throttler.Release();
				}
			}

			foreach (var shardedCollection in shardedCollections.Values.Where(_ => !_.Dropped))
			{
				await throttler.WaitAsync(token);
				collTasks.Add(runCollStat(shardedCollection));
			}
			
			var sb = new StringBuilder();

			foreach (var newShardedCollection in await Task.WhenAll(collTasks))
			{
				var totalSize = newShardedCollection.Stats.Size;
				if (totalSize == 0)
					totalSize = 1;

				var distributionMap = allShards.ToDictionary(_ => _, _ => 0.0);
				foreach (var pair in newShardedCollection.Stats.Shards)
					distributionMap[pair.Key.ToString()] = (double) pair.Value.Size * 100 / totalSize;

				var distribution = distributionMap
					.OrderBy(_ => _.Key)
					.Select(_ => $"{_.Value:F0}%");

				sb.AppendLine();
				sb.AppendLine($"\t<!-- totalSize: {newShardedCollection.Stats.Size.ByteSize()} storageSize: {newShardedCollection.Stats.StorageSize.ByteSize()} distribution: {string.Join(" ", distribution)} -->");
				sb.AppendLine($"\t<!-- key: {newShardedCollection.Info.Key.ToJson(_jsonWriterSettings)} -->");
				sb.AppendLine($"\t<Interval nameSpace=\"{newShardedCollection.Info.Id}\" bounds=\"\"");
				sb.AppendLine($"\t\tpreSplit=\"chunks\" correction=\"true\" zones=\"{defaultZones}\" />");
			}
			
			Console.WriteLine("New intervals:");
			Console.WriteLine(sb);
		}

		public class NewShardedCollection
		{
			public ShardedCollectionInfo Info { get; set; }
			public CollStatsResult Stats { get; set; }
		}
	}
}