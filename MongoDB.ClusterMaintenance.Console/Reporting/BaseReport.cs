using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.ClusterMaintenance.Models;
using MongoDB.ClusterMaintenance.MongoCommands;

namespace MongoDB.ClusterMaintenance.Reporting
{
	public abstract class BaseReport
	{
		protected readonly SizeRenderer SizeRenderer;
	
		public readonly Dictionary<ShardIdentity, ShardRow> Rows = new Dictionary<ShardIdentity, ShardRow>();
			
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

		public BaseReport(SizeRenderer sizeRenderer)
		{
			SizeRenderer = sizeRenderer;
		}

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

		public void CalcBottom()
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

		public StringBuilder Render()
		{
			var sb = new StringBuilder();

			AppendHeader(sb, "",
				"", "", "", 
				"Deviation", "\\", "\\",
				"Unsharded", "\\", "\\",
				"Sharded", "\\", "\\");
				
			AppendHeader(sb, "Shard name",
				"Size", "Storage", "Index", 
				"Size", "Storage", "Index",
				"Size", "Storage", "Index",
				"Size", "Storage", "Index");

			foreach (var p in Rows.OrderBy(_ => _.Key))
			{
				var row = p.Value;
				
				AppendRow(sb, (string)p.Key,
					row.Size, row.StorageSize, row.IndexSize,
					row.DeviationSize, row.DeviationStorageSize, row.DeviationIndexSize,
					row.UnShardedSize, row.UnShardedStorageSize, row.UnShardedIndexSize,
					row.ShardedSize, row.ShardedStorageSize, row.ShardedIndexSize);
			}
			
			AppendRow(sb, "<total>",
				TotalSize, TotalStorageSize, TotalIndexSize,
				null, null, null,
				TotalUnShardedSize, TotalUnShardedStorageSize, TotalUnShardedIndexSize, 
				TotalShardedSize, TotalShardedStorageSize, TotalShardedIndexSize);
			
			AppendRow(sb, "<average>",
				AverageSize, AverageStorageSize, AverageIndexSize, 
				null, null, null, 
				null, null, null,
				null, null, null);

			return sb;
		}

		protected abstract void AppendRow(StringBuilder sb, string rowTitle, params long?[] cells);

		protected abstract void AppendHeader(StringBuilder sb, params string[] cells);
	}
}