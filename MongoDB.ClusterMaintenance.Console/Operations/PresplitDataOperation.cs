using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.ClusterMaintenance.Config;
using MongoDB.Driver;
using NLog;

namespace MongoDB.ClusterMaintenance.Operations
{
	public class PresplitDataOperation : IOperation
	{
		private readonly IReadOnlyList<Interval> _intervals;
		private readonly IConfigDbRepositoryProvider _configDb;
			
		private static readonly Logger _log = LogManager.GetCurrentClassLogger();

		public PresplitDataOperation(IConfigDbRepositoryProvider configDb, IReadOnlyList<Interval> intervals)
		{
			_configDb = configDb;
			_intervals = intervals;
		}

		public async Task Run(CancellationToken token)
		{
			var allCommands = new StringBuilder();
			foreach (var interval in _intervals)
			{
				allCommands.AppendFormat("//presplit commands for {0}:", interval.Namespace.FullName);
				var preSplit = interval.PreSplit;

				if (preSplit == PreSplitType.Auto)
				{
					var totalChunks = await _configDb.Chunks
						.ByNamespace(interval.Namespace)
						.ChunkFrom(interval.ChunkFrom)
						.ChunkTo(interval.ChunkTo).Count();

					preSplit = totalChunks / interval.Zones.Count < 100 ? PreSplitType.Interval : PreSplitType.Chunks;
				}

				switch (preSplit)
				{
					case PreSplitType.Interval:
						await presplitData(interval, token, allCommands);
						break;
					case PreSplitType.Chunks:
						await distributeCollection(interval, token, allCommands);
						break;
					
					case PreSplitType.Auto:
					default:
						throw new NotSupportedException($"unexpected PreSplitType:{preSplit}");
				}
			}

			Console.WriteLine(allCommands);
		}

		private async Task presplitData(Interval interval, CancellationToken token, StringBuilder allCommands)
		{
			var collInfo = await _configDb.Collections.Find(interval.Namespace);

			if (collInfo == null)
				throw new InvalidOperationException($"collection {interval.Namespace.FullName} not sharded");

			var chunkFrom = await _configDb.Chunks.Find(interval.ChunkFrom);
			if (chunkFrom == null)
				throw new InvalidOperationException($"start chunk {interval.ChunkFrom} not found");
			if (!chunkFrom.Namespace.Equals(interval.Namespace))
				throw new InvalidOperationException(
					$"start chunk {interval.ChunkFrom} in other collection {chunkFrom.Namespace}");

			var chunkTo = await _configDb.Chunks.Find(interval.ChunkTo);
			if (chunkTo == null)
				throw new InvalidOperationException($"end chunk {interval.ChunkTo} not found");
			if (!chunkTo.Namespace.Equals(interval.Namespace))
				throw new InvalidOperationException(
					$"end chunk {interval.ChunkTo} in other collection {chunkTo.Namespace}");

			var internalBounds = BsonSplitter.SplitFirstValue(chunkFrom.Max, chunkTo.Min, interval.Zones.Count)
				.ToList();

			var allBounds = new List<BsonDocument>(internalBounds.Count + 2);

			allBounds.Add(chunkFrom.Max);
			allBounds.AddRange(internalBounds);
			allBounds.Add(chunkTo.Min);

			var jsonSettings = new JsonWriterSettings()
				{Indent = false, GuidRepresentation = GuidRepresentation.Unspecified};

			allCommands.AppendLine("//Split Commands:");
			foreach (var bound in internalBounds)
			{
				_log.Info("Split by: {0}", bound);
				var boundJson = bound.ToJson(jsonSettings);
				var splitCommand = $"sh.splitAt( \"{interval.Namespace.FullName}\", {boundJson} );";
				allCommands.AppendLine(splitCommand);
			}

			var zoneIndex = 0;
			allCommands.AppendLine("//Tag Range Commands:");
			foreach (var range in allBounds.Zip(allBounds.Skip(1), (min, max) => new {min, max}))
			{
				var zoneName = interval.Zones[zoneIndex];
				zoneIndex++;

				var min = range.min.ToJson(jsonSettings);
				var max = range.max.ToJson(jsonSettings);
				_log.Info("Zone: {0} from {1} to {2}", zoneName, min, max);

				var addTagRangeCommand =
					$"sh.addTagRange( \"{interval.Namespace.FullName}\", {min}, {max}, \"{zoneName}\");";
				allCommands.AppendLine(addTagRangeCommand);
			}
		}
		
		private async Task distributeCollection(Interval interval, CancellationToken token, StringBuilder commands)
		{
			var collInfo = await _configDb.Collections.Find(interval.Namespace);

			if (collInfo == null)
				throw new InvalidOperationException($"collection {interval.Namespace.FullName} not sharded");

			var filtered = _configDb.Chunks
				.ByNamespace(interval.Namespace)
				.ChunkFrom(interval.ChunkFrom)
				.ChunkTo(interval.ChunkTo);

			var chunks = await (await filtered.Find()).ToListAsync(token);

			foreach (var part in chunks.Split(interval.Zones.Count).Select((items, order) => new {Items = items, Order = order}))
			{
				var zoneName = interval.Zones[part.Order];
				var min = part.Items.First().Min.ToJson(new JsonWriterSettings() {Indent = false});
				var max = part.Items.Last().Max.ToJson(new JsonWriterSettings() {Indent = false});
				_log.Info("Zone: {0} ({1}) from {2} to {3}", zoneName, part.Items.Count, min, max);

				var addTagRangeCommand = $"sh.addTagRange( \"{interval.Namespace.FullName}\", {min}, {max}, \"{zoneName}\");";
				commands.AppendLine(addTagRangeCommand);
			}
		}
	}
}