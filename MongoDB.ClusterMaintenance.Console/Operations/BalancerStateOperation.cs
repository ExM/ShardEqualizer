using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.ClusterMaintenance.Config;
using MongoDB.ClusterMaintenance.Models;
using MongoDB.Driver;
using NLog;

namespace MongoDB.ClusterMaintenance.Operations
{
	public class BalancerStateOperation: IOperation
	{
		private static readonly Logger _log = LogManager.GetCurrentClassLogger();
		
		private readonly IConfigDbRepositoryProvider _configDb;
		private readonly IReadOnlyList<Interval> _intervals;

		public BalancerStateOperation(IConfigDbRepositoryProvider configDb, IReadOnlyList<Interval> intervals)
		{
			_intervals = intervals;
			_configDb = configDb;
		}
	
		public async Task Run(CancellationToken token)
		{
			var shards = await _configDb.Shards.GetAll();
			var totalUnMovedChunks = 0;
			
			foreach (var interval in _intervals.Where(_ => _.Selected))
			{
				_log.Info("Scan interval: {0}", interval.Namespace);
				
				var currentTags = new HashSet<TagIdentity>(interval.Zones);
				var tagRanges = await _configDb.Tags.Get(interval.Namespace);
				tagRanges = tagRanges.Where(_ => currentTags.Contains(_.Tag)).ToList();

				foreach (var tagRange in tagRanges)
				{
					var validShards = new HashSet<ShardIdentity>(shards.Where(_ => _.Tags.Contains(tagRange.Tag)).Select(_ => _.Id));
					
					var chunks = await (await _configDb.Chunks.ByNamespace(interval.Namespace)
						.From(tagRange.Min).To(tagRange.Max).Find()).ToListAsync(token);

					var unMovedChunks = chunks.Count(_ => _.Jumbo != true && !validShards.Contains(_.Shard));
					_log.Info("  tag range {0} contains {1} unmoved chunks", tagRange.Tag, unMovedChunks);
					totalUnMovedChunks += unMovedChunks;
				}
			}
			
			_log.Info("Total unmoved chunks: {0}", totalUnMovedChunks);
		}
	}
}