using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization.Attributes;
using ShardEqualizer.LocalStoring;

namespace ShardEqualizer
{
	public class ClusterSettingsService
	{
		private readonly SettingsRepository _repo;
		private readonly ProgressRenderer _progressRenderer;
		private readonly ILocalStore<Container> _store;

		public ClusterSettingsService(
			SettingsRepository repo,
			ProgressRenderer progressRenderer,
			LocalStoreProvider storeProvider)
		{
			_repo = repo;
			_progressRenderer = progressRenderer;
			_store = storeProvider.Get("settings", uploadChunksize);
		}

		public async Task<long> GetChunkSize(CancellationToken token)
		{
			await using var reporter = _progressRenderer.Start("Load settings");

			var container = await _store.Get(token);

			return container.ChunkSize;
		}

		private async Task<Container> uploadChunksize(CancellationToken token)
		{
			return new Container()
			{
				ChunkSize = await _repo.GetChunksize(token)
			};
		}

		private class Container
		{
			[BsonElement("chunkSize"), BsonRequired]
			public long ChunkSize { get; set; }
		}
	}
}
