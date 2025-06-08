using MongoDB.Bson.Serialization.Attributes;
using ShardEqualizer.Caching;
using ShardEqualizer.Contracts.UI;
using ShardEqualizer.DAL.Repositories;

namespace ShardEqualizer.ShardedClusterViews;

public class ClusterSettingsView
{
	private readonly ILazyServiceProvider _serviceProvider;
	private readonly IProgressCollector _progressRenderer;
	private readonly IBsonCache<Container> _cache;

	public ClusterSettingsView(
		ILazyServiceProvider serviceProvider,
		IProgressCollector progressRenderer,
		IBsonCacheFactory cacheFactory)
	{
		_serviceProvider = serviceProvider;
		_progressRenderer = progressRenderer;
		_cache = cacheFactory.Get(["settings"], UploadData);
	}

	public async Task<long> GetChunkSize(CancellationToken token)
	{
		return (await _cache.Get(token)).ChunkSize;
	}

	private async Task<Container> UploadData(CancellationToken token)
	{
		var repo = _serviceProvider.Resolve<SettingsRepository>();
		await using var reporter = _progressRenderer.Start("Load settings");
		
		return new Container()
		{
			ChunkSize = await repo.GetChunksize(token)
		};
	}

	private class Container
	{
		[BsonElement("chunkSize"), BsonRequired]
		public required long ChunkSize { get; init; }
	}
}