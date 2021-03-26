using System.Collections.Concurrent;
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
	public class TagRangeService
	{
		private readonly TagRangeRepository _repo;
		private readonly ProgressRenderer _progressRenderer;
		private readonly INsLocalStore<Container> _store;

		public TagRangeService(
			TagRangeRepository repo,
			ProgressRenderer progressRenderer,
			LocalStoreProvider storeProvider)
		{
			_repo = repo;
			_progressRenderer = progressRenderer;

			_store = storeProvider.Get("tagRanges", uploadTagRanges);
		}

		public async Task<IReadOnlyDictionary<CollectionNamespace, IReadOnlyList<TagRange>>> Get(IEnumerable<CollectionNamespace> nss, CancellationToken token)
		{
			var nsList = nss.ToList();

			await using var reporter = _progressRenderer.Start($"Load tag ranges", nsList.Count);

			async Task<(CollectionNamespace ns, IReadOnlyList<TagRange> tagRanges)> getTagRanges(CollectionNamespace ns, CancellationToken t)
			{
				var container = await _store.Get(ns, t);
				reporter.Increment();
				return (ns, container.TagRanges);
			}

			var pairs = await nsList.ParallelsAsync(getTagRanges, 32, token);

			return pairs.ToDictionary(_ => _.ns, _ => _.tagRanges);
		}

		private async Task<Container> uploadTagRanges(CollectionNamespace ns, CancellationToken token)
		{
			return new Container()
			{
				TagRanges = await _repo.Get(ns, token)
			};
		}

		private class Container
		{
			[BsonElement("tagRanges")]
			public IReadOnlyList<TagRange> TagRanges { get; set; }
		}
	}
}
