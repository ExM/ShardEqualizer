using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;
using ShardEqualizer.ByteSizeRendering;
using ShardEqualizer.ConfigServices;
using ShardEqualizer.DAL.Models;
using ShardEqualizer.ScriptGen;
using ShardEqualizer.ShardedClusterViews;
using ShardEqualizer.ShardedClusterViews.Models;

namespace ShardEqualizer.Operations
{
	public class ConfigUpdateOperation: IOperation
	{
		private readonly ShardedCollectionInfoView _shardedCollectionInfoView;
		private readonly CollectionStatisticView _collectionStatisticView;
		private readonly IReadOnlyList<Interval> _intervals;

		private Dictionary<CollectionNamespace, ShardedCollectionInfo> _shardedCollections;
		private IReadOnlyList<NewShardedCollection> _newShardedCollection;

		public ConfigUpdateOperation(
			ShardedCollectionInfoView shardedCollectionInfoView,
			CollectionStatisticView collectionStatisticView,
			IReadOnlyList<Interval> intervals)
		{
			_shardedCollectionInfoView = shardedCollectionInfoView;
			_collectionStatisticView = collectionStatisticView;
			_intervals = intervals;
		}

		private void analyseIntervals()
		{
			foreach (var ns in _intervals.Select(_ => _.Namespace))
			{
				if (_shardedCollections.TryGetValue(ns, out var shardedCollection))
				{
					_shardedCollections.Remove(ns);
				}
				else
				{
					Console.WriteLine("\tcollection '{0}' not sharded", ns);
				}
			}
		}

		public async Task Run(CancellationToken token)
		{
			_shardedCollections = new Dictionary<CollectionNamespace, ShardedCollectionInfo>(
				await _shardedCollectionInfoView.Get(token));

			analyseIntervals();

			var collStats = await _collectionStatisticView.Get(_shardedCollections.Keys, token);

			_newShardedCollection = _shardedCollections.Keys
				.Select(_ => new NewShardedCollection()
				{
					Info = _shardedCollections[_],
					Stats = collStats[_]
				})
				.ToList();

			if (_newShardedCollection.Count == 0)
			{
				Console.WriteLine("new sharded collections not found");
				return;
			}

			var sb = new StringBuilder();

			foreach (var newShardedCollection in _newShardedCollection)
			{
				sb.AppendLine();
				sb.AppendLine($"\t<!-- totalSize: {newShardedCollection.Stats.Size.ByteSize()} " +
				              $"storageSize: {newShardedCollection.Stats.StorageSize.ByteSize()} " +
				              $"key: {ShellJsonWriter.AsJson(newShardedCollection.Info.Key)} -->");
				sb.AppendLine($"\t<Interval nameSpace=\"{newShardedCollection.Info.Id}\" />");
			}

			Console.WriteLine("New intervals:");
			Console.WriteLine(sb);
		}

		public class NewShardedCollection
		{
			public ShardedCollectionInfo Info { get; set; }
			public CollectionStatistics Stats { get; set; }
		}
	}
}
