namespace MongoDB.ClusterMaintenance
{
	public interface IConfigDbRepositoryProvider
	{
		ChunkRepository Chunks { get; }
		CollectionRepository Collections { get; }
	}
}