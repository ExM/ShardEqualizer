using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization.Attributes;
using ShardEqualizer.ConfigRepositories;
using ShardEqualizer.LocalStoring;
using ShardEqualizer.Models;
using ShardEqualizer.UI;

namespace ShardEqualizer.ConfigServices
{
	public class ShardListService
	{
		private readonly ShardRepository _repo;
		private readonly ProgressRenderer _progressRenderer;
		private readonly ILocalStore<Container> _store;

		public ShardListService(
			ShardRepository repo,
			ProgressRenderer progressRenderer,
			LocalStoreProvider storeProvider)
		{
			_repo = repo;
			_progressRenderer = progressRenderer;
			_store = storeProvider.Get("shards", uploadData);
		}

		public async Task<IReadOnlyCollection<Shard>> Get(CancellationToken token)
		{
			var result = await _store.Get(token);
			return result.Shards;
		}

		private async Task<Container> uploadData(CancellationToken token)
		{
			await using var reporter = _progressRenderer.Start("Load shard list");
			var result = await _repo.GetAll(token);
			reporter.SetCompleteMessage($"found {result.Count} shards.");

			return new Container()
			{
				Shards = result
			};
		}

		private class Container
		{
			[BsonElement("shards"), BsonRequired]
			public IReadOnlyCollection<Shard> Shards { get; set; }
		}
	}
}
