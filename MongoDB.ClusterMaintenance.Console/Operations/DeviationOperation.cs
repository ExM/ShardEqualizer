using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.ClusterMaintenance.Config;
using MongoDB.ClusterMaintenance.Models;
using MongoDB.ClusterMaintenance.MongoCommands;
using MongoDB.Driver;
using NLog;

namespace MongoDB.ClusterMaintenance.Operations
{
	public class DeviationOperation: IOperation
	{
		private static readonly Logger _log = LogManager.GetCurrentClassLogger();
		
		private readonly IMongoClient _mongoClient;
		private readonly ScaleSuffix _scaleSuffix;

		public DeviationOperation(IMongoClient mongoClient, ScaleSuffix scaleSuffix)
		{
			_mongoClient = mongoClient;
			_scaleSuffix = scaleSuffix;
		}

		public async Task Run(CancellationToken token)
		{
			var allDatabaseNames = await _mongoClient.ListDatabaseNames().ToListAsync(token);

			var allCollectionNames = new List<CollectionNamespace>();
			
			foreach (var dbName in allDatabaseNames.Except(new []{ "admin", "config" }))
			{
				_log.Info("db: {0}", dbName);
				
				var db = _mongoClient.GetDatabase(dbName);
				var collNames = await db.ListCollectionNames().ToListAsync(token);

				allCollectionNames.AddRange(collNames.Select(_ => new CollectionNamespace(dbName, _)));
			}
			
			_log.Info("Found: {0} collections", allCollectionNames.Count);

			var result = await allCollectionNames.ParallelsAsync(runCollStats, 32, token);

			var report = new Report();
			foreach (var collStats in result)
			{
				report.Append(collStats);
			}
			report.Finalize();
			
			var sizeRenderer = new SizeRenderer("F2", _scaleSuffix);
			
			var sb = report.Render(sizeRenderer);
			
			Console.WriteLine("Report as CSV:");
			Console.WriteLine();
			Console.WriteLine(sb);
		}
		
		private async Task<CollStatsResult> runCollStats(CollectionNamespace ns, CancellationToken token)
		{
			_log.Info("collection: {0}", ns);
			var db = _mongoClient.GetDatabase(ns.DatabaseNamespace.DatabaseName);
			return await db.CollStats(ns.CollectionName, 1, token);
		}
		
		private class Report
		{
			public Dictionary<ShardIdentity, ShardRow> Rows = new Dictionary<ShardIdentity, ShardRow>();
			
			public long TotalSize => TotalShardedSize + TotalUnShardedSize;
			public long TotalStorageSize => TotalShardedStorageSize + TotalUnShardedStorageSize;
			public long TotalIndexSize => TotalShardedIndexSize + TotalUnShardedIndexSize;
			
			public long TotalShardedSize;
			public long TotalShardedStorageSize;
			public long TotalShardedIndexSize;
			
			public long TotalUnShardedSize;
			public long TotalUnShardedStorageSize;
			public long TotalUnShardedIndexSize;

			public long AverageSize;
			public long AverageStorageSize;
			public long AverageIndexSize;

			public void Append(CollStatsResult collStats)
			{
				if (collStats.Sharded)
				{
					foreach(var shardCollStats in collStats.Shards)
						ensureRow(shardCollStats.Key).AppendSharded(shardCollStats.Value);
				}
				else
				{
					var shardName = collStats.Primary;
					ensureRow(shardName).AppendUnSharded(collStats);
				}
			}

			private ShardRow ensureRow(ShardIdentity shardName)
			{
				if (Rows.ContainsKey(shardName))
					return Rows[shardName];
				var row = new ShardRow(this);
				Rows.Add(shardName, row);
				return row;
			}

			public void Finalize()
			{
				TotalShardedSize = Rows.Values.Sum(_ => _.ShardedSize);
				TotalShardedStorageSize = Rows.Values.Sum(_ => _.ShardedStorageSize);
				TotalShardedIndexSize = Rows.Values.Sum(_ => _.ShardedIndexSize);
				
				TotalUnShardedSize = Rows.Values.Sum(_ => _.UnShardedSize);
				TotalUnShardedStorageSize = Rows.Values.Sum(_ => _.UnShardedStorageSize);
				TotalUnShardedIndexSize = Rows.Values.Sum(_ => _.UnShardedIndexSize);

				AverageSize = TotalSize / Rows.Count;
				AverageStorageSize = TotalStorageSize / Rows.Count;
				AverageIndexSize = TotalIndexSize / Rows.Count;
			}

			public StringBuilder Render(SizeRenderer sizeRenderer)
			{
				var sb = new StringBuilder();

				appendRow(sb, "",
					"", "", "", 
					"Deviation", "Deviation", "Deviation",
					"Unsharded", "Unsharded", "Unsharded",
					"Sharded", "Sharded", "Sharded");
				
				appendRow(sb, "Shard name",
					"Size", "Storage", "Index", 
					"Size", "Storage", "Index",
					"Size", "Storage", "Index",
					"Size", "Storage", "Index");

				foreach (var row in Rows.OrderBy(_ => _.Key))
				{
					row.Value.Render(sb, sizeRenderer, (string)row.Key);
				}
				
				appendRow(sb, "<total>",
					sizeRenderer.Render(TotalSize), sizeRenderer.Render(TotalStorageSize), sizeRenderer.Render(TotalIndexSize),
					"", "", "",
					sizeRenderer.Render(TotalUnShardedSize), sizeRenderer.Render(TotalUnShardedStorageSize), sizeRenderer.Render(TotalUnShardedIndexSize), 
					sizeRenderer.Render(TotalShardedSize), sizeRenderer.Render(TotalShardedStorageSize), sizeRenderer.Render(TotalShardedIndexSize));
				
				appendRow(sb, "<average>",
					sizeRenderer.Render(AverageSize), sizeRenderer.Render(AverageStorageSize), sizeRenderer.Render(AverageIndexSize), 
					"", "", "", 
					"", "", "",
					"", "", "");

				return sb;
			}

			private void appendRow(StringBuilder sb, params string[] cells)
			{
				sb.AppendLine(string.Join(";", cells));
			}
		}

		
		private class ShardRow
		{
			private readonly Report _report;
			
			public long Size => ShardedSize + UnShardedSize;
			public long StorageSize => ShardedStorageSize + UnShardedStorageSize;
			public long IndexSize => ShardedIndexSize + UnShardedIndexSize;
			
			public long ShardedSize;
			public long ShardedStorageSize;
			public long ShardedIndexSize;
			
			public long UnShardedSize;
			public long UnShardedStorageSize;
			public long UnShardedIndexSize;
			
			public long DeviationSize => Size - _report.AverageSize;
			public long DeviationStorageSize => StorageSize - _report.AverageStorageSize;
			public long DeviationIndexSize => IndexSize - _report.AverageIndexSize;

			public ShardRow(Report report)
			{
				_report = report;
			}
			
			public void AppendSharded(CollStats collStats)
			{
				ShardedSize += collStats.Size;
				ShardedStorageSize += collStats.StorageSize;
				ShardedIndexSize += collStats.TotalIndexSize;
			}
			
			public void AppendUnSharded(CollStats collStats)
			{
				UnShardedSize += collStats.Size;
				UnShardedStorageSize += collStats.StorageSize;
				UnShardedIndexSize += collStats.TotalIndexSize;
			}

			public void Render(StringBuilder sb, SizeRenderer sizeRenderer, string shardName)
			{
				appendRow(sb, shardName,
					sizeRenderer.Render(Size), sizeRenderer.Render(StorageSize), sizeRenderer.Render(IndexSize), 
					sizeRenderer.Render(DeviationSize), sizeRenderer.Render(DeviationStorageSize), sizeRenderer.Render(DeviationIndexSize),
					sizeRenderer.Render(UnShardedSize), sizeRenderer.Render(UnShardedStorageSize), sizeRenderer.Render(UnShardedIndexSize), 
					sizeRenderer.Render(ShardedSize), sizeRenderer.Render(ShardedStorageSize), sizeRenderer.Render(ShardedIndexSize));
			}
			
			private void appendRow(StringBuilder sb, params string[] cells)
			{
				sb.AppendLine(string.Join(";", cells));
			}
		}
	}
}