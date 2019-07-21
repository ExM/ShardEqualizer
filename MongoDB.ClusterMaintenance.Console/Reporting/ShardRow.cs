using System.Text;
using MongoDB.ClusterMaintenance.MongoCommands;

namespace MongoDB.ClusterMaintenance.Reporting
{
	public class ShardRow
	{
		private readonly BaseReport _baseReport;

		public long Size => ShardedSize + UnShardedSize;
		public long StorageSize => ShardedStorageSize + UnShardedStorageSize;
		public long IndexSize => ShardedIndexSize + UnShardedIndexSize;

		public long ShardedSize;
		public long ShardedStorageSize;
		public long ShardedIndexSize;

		public long UnShardedSize;
		public long UnShardedStorageSize;
		public long UnShardedIndexSize;

		public long DeviationSize => Size - _baseReport.AverageSize;
		public long DeviationStorageSize => StorageSize - _baseReport.AverageStorageSize;
		public long DeviationIndexSize => IndexSize - _baseReport.AverageIndexSize;

		public ShardRow(BaseReport baseReport)
		{
			_baseReport = baseReport;
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
	}
}