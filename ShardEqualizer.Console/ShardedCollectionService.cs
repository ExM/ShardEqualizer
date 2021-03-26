using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Driver;
using ShardEqualizer.LocalStoring;
using ShardEqualizer.Models;

namespace ShardEqualizer
{
	public class ShardedCollectionService
	{
		private readonly CollectionRepository _repo;
		private readonly ProgressRenderer _progressRenderer;
		private readonly ILocalStore<Container> _store;

		public ShardedCollectionService(
			CollectionRepository repo,
			ProgressRenderer progressRenderer,
			LocalStoreProvider storeProvider)
		{
			_repo = repo;
			_progressRenderer = progressRenderer;
			_store = storeProvider.Get("shardedCollections", uploadData);
		}

		public async Task<IReadOnlyDictionary<CollectionNamespace, ShardedCollectionInfo>> Get(CancellationToken token)
		{
			var container = await _store.Get(token);
			return container.ShardedCollection;
		}

		private async Task<Container> uploadData(CancellationToken token)
		{
			await using var reporter = _progressRenderer.Start("Load sharded collections");
			var result = await _repo.FindAll(false, token); //TODO maybe skip dropped items
			reporter.SetCompleteMessage($"found {result.Count} collections.");

			return new Container()
			{
				ShardedCollection = result.ToDictionary(_ => _.Id)
			};
		}

		private class Container
		{
			[BsonElement("shardedCollections"), BsonDictionaryOptions(DictionaryRepresentation.Document), BsonIgnoreIfNull]
			public Dictionary<CollectionNamespace, ShardedCollectionInfo> ShardedCollection { get; set; }
		}
	}
}
