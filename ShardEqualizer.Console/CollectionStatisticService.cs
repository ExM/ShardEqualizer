using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Driver;
using ShardEqualizer.LocalStoring;
using ShardEqualizer.MongoCommands;
using ShardEqualizer.ShortModels;

namespace ShardEqualizer
{
	public class CollectionStatisticService
	{
		private readonly IMongoClient _mongoClient;
		private readonly ProgressRenderer _progressRenderer;
		private readonly INsLocalStore<CollectionStatistics> _store;

		public CollectionStatisticService(
			IMongoClient mongoClient,
			ProgressRenderer progressRenderer,
			LocalStoreProvider storeProvider)
		{
			_mongoClient = mongoClient;
			_progressRenderer = progressRenderer;

			_store = storeProvider.Get("collStats", uploadData);
		}

		public async Task<IReadOnlyDictionary<CollectionNamespace, CollectionStatistics>> Get(IEnumerable<CollectionNamespace> nss, CancellationToken token)
		{
			var nsList = nss.ToList();
			await using var reporter = _progressRenderer.Start($"Load collection statistics", nsList.Count);

			async Task<(CollectionNamespace ns, CollectionStatistics collStat)> getCollStat(CollectionNamespace ns, CancellationToken t)
			{
				var result = await _store.Get(ns, t);
				reporter.Increment();
				return (ns, result);
			}

			var results = await nsList.ParallelsAsync(getCollStat, 32, token);
			return results.ToDictionary(_ => _.ns, _ => _.collStat);
		}

		private async Task<CollectionStatistics> uploadData(CollectionNamespace ns, CancellationToken token)
		{
			var db = _mongoClient.GetDatabase(ns.DatabaseNamespace.DatabaseName);
			var collStat = await db.CollStats(ns.CollectionName, 1, token);
			return new CollectionStatistics(collStat);
		}
	}
}
