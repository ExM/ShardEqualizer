using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.ClusterMaintenance.Config;
using MongoDB.ClusterMaintenance.Models;
using MongoDB.ClusterMaintenance.MongoCommands;
using MongoDB.Driver;
using NLog;

namespace MongoDB.ClusterMaintenance.Operations
{
	public class PresplitDataOperation : IOperation
	{
		private readonly IReadOnlyList<Interval> _intervals;
		private readonly CommandPlanWriter _commandPlanWriter;
		private readonly IConfigDbRepositoryProvider _configDb;
			
		private static readonly Logger _log = LogManager.GetCurrentClassLogger();

		public PresplitDataOperation(IConfigDbRepositoryProvider configDb, IReadOnlyList<Interval> intervals, CommandPlanWriter commandPlanWriter)
		{
			_configDb = configDb;
			_intervals = intervals;
			_commandPlanWriter = commandPlanWriter;
		}

		public async Task Run(CancellationToken token)
		{
			foreach (var interval in _intervals)
			{
				_commandPlanWriter.Comment($"presplit commands for {interval.Namespace.FullName}");
				var preSplit = interval.PreSplit;

				if (preSplit == PreSplitType.Auto)
				{
					if (interval.Min.HasValue && interval.Max.HasValue)
					{
						var totalChunks = await _configDb.Chunks
							.ByNamespace(interval.Namespace)
							.From(interval.Min)
							.To(interval.Max).Count();

						preSplit = totalChunks / interval.Zones.Count < 100
							? PreSplitType.Interval
							: PreSplitType.Chunks;
						
						_log.Info("detect presplit mode of {0} with total chunks {1}", interval.Namespace.FullName, totalChunks);
					}
					else
					{
						preSplit = PreSplitType.Chunks;
						
						_log.Info("detect presplit mode of {0} without bounds", interval.Namespace.FullName);
					}
				}
				
				await removeOldTagRanges(interval);

				_log.Info("presplit data of {0} with mode {1}", interval.Namespace.FullName, preSplit);
				
				switch (preSplit)
				{
					case PreSplitType.Interval:
						await presplitData(interval, token);
						break;
					case PreSplitType.Chunks:
						await distributeCollection(interval, token);
						break;
					
					case PreSplitType.Auto:
					default:
						throw new NotSupportedException($"unexpected PreSplitType:{preSplit}");
				}
			}
		}

		private async Task removeOldTagRanges(Interval interval)
		{
			var tagRanges = await _configDb.Tags.Get(interval.Namespace);
			if (interval.Min.HasValue && interval.Max.HasValue)
				tagRanges = tagRanges.Where(r => interval.Min.Value <= r.Min && r.Max <= interval.Max.Value).ToList()
					.AsReadOnly();

			foreach (var tagRange in tagRanges)
				_commandPlanWriter.RemoveTagRange(interval.Namespace, tagRange.Min, tagRange.Max, tagRange.Tag);
		}

		private async Task presplitData(Interval interval, CancellationToken token)
		{
			var collInfo = await _configDb.Collections.Find(interval.Namespace);

			if (collInfo == null)
				throw new InvalidOperationException($"collection {interval.Namespace.FullName} not sharded");
			
			if(interval.Min == null || interval.Max == null)
				throw new InvalidOperationException($"collection {interval.Namespace.FullName} - bounds not found in configuration");
			
			var internalBounds = BsonSplitter.SplitFirstValue(interval.Min.Value, interval.Max.Value, interval.Zones.Count)
				.ToList();

			var allBounds = new List<BsonBound>(internalBounds.Count + 2);

			allBounds.Add(interval.Min.Value);
			allBounds.AddRange(internalBounds);
			allBounds.Add(interval.Max.Value);

			var zoneIndex = 0;
			foreach (var range in allBounds.Zip(allBounds.Skip(1), (min, max) => new {min, max}))
			{
				var zoneName = interval.Zones[zoneIndex];
				zoneIndex++;
				
				_commandPlanWriter.AddTagRange(interval.Namespace, range.min, range.max, zoneName);
			}
		}
		
		private async Task distributeCollection(Interval interval, CancellationToken token)
		{
			var collInfo = await _configDb.Collections.Find(interval.Namespace);

			if (collInfo == null)
				throw new InvalidOperationException($"collection {interval.Namespace.FullName} not sharded");

			var filtered = _configDb.Chunks
				.ByNamespace(interval.Namespace)
				.From(interval.Min)
				.To(interval.Max);

			var chunks = await (await filtered.Find()).ToListAsync(token);
			foreach (var part in chunks.Split(interval.Zones.Count).Select((items, order) => new {Items = items, Order = order}))
			{
				var zoneName = interval.Zones[part.Order];
				
				_commandPlanWriter.AddTagRange(interval.Namespace, part.Items.First().Min, part.Items.Last().Max, zoneName);
			}
		}
	}
}