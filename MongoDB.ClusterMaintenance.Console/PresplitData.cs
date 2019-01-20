using CommandLine;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using NLog;

namespace MongoDB.ClusterMaintenance
{
	[Verb("presplit", HelpText = "distribute data by zones with splitting existing chunks")]
	public class PresplitData : BaseOptions
	{
		private static readonly Logger _log = LogManager.GetCurrentClassLogger();
		
		[Option("zones", Separator = ',', Required = true, HelpText = "Name of zones.")]
		public IList<string> Zones { get; set; }
		
		[Option("chunkFrom", Required = true, HelpText = "Chunk scan left boundary.")]
		public string ChunkFrom { get; set; }

		[Option("chunkTo", Required = true, HelpText = "Chunk scan right boundary.")]
		public string ChunkTo { get; set; }

		public override async Task Run(CancellationToken token)
		{
			var collInfo = await ConfigDb.Collections.Find(CollectionNamespace);

			if (collInfo == null)
				throw new InvalidOperationException($"collection {Database}.{Collection} not sharded");

			var chunkFrom = await ConfigDb.Chunks.Find(ChunkFrom);
			if(chunkFrom == null)
				throw new InvalidOperationException($"start chunk {ChunkFrom} not found");
			if(!chunkFrom.Namespace.Equals(CollectionNamespace))
				throw new InvalidOperationException($"start chunk {ChunkFrom} in other collection {chunkFrom.Namespace}");
			
			var chunkTo = await ConfigDb.Chunks.Find(ChunkTo);
			if(chunkTo == null)
				throw new InvalidOperationException($"end chunk {ChunkTo} not found");
			if(!chunkTo.Namespace.Equals(CollectionNamespace))
				throw new InvalidOperationException($"end chunk {ChunkTo} in other collection {chunkTo.Namespace}");
			
			var internalBounds = BsonSplitter.SplitFirstValue(chunkFrom.Max, chunkTo.Min, Zones.Count).ToList();
			
			var allBounds = new List<BsonDocument>(internalBounds.Count + 2);

			allBounds.Add(chunkFrom.Max);
			allBounds.AddRange(internalBounds);
			allBounds.Add(chunkTo.Min);
			
			var splitCommands = new StringBuilder();
			var tagRangeCommands = new StringBuilder();
			var jsonSettings = new JsonWriterSettings() {Indent = false, GuidRepresentation = GuidRepresentation.Unspecified};
			
			foreach (var bound in internalBounds)
			{
				_log.Info("Split by: {0}", bound);
				var boundJson = bound.ToJson(jsonSettings);
				var splitCommand = $"sh.splitAt( \"{CollectionNamespace.FullName}\", {boundJson} );";
				splitCommands.AppendLine(splitCommand);
			}
			
			var zoneIndex = 0;
			
			foreach (var interval in allBounds.Zip(allBounds.Skip(1), (min, max) => new { min, max}))
			{
				var zoneName = Zones[zoneIndex];
				zoneIndex++;
				
				var min = interval.min.ToJson(jsonSettings);
				var max = interval.max.ToJson(jsonSettings);
				_log.Info("Zone: {0} from {1} to {2}", zoneName, min, max);
				
				var addTagRangeCommand = $"sh.addTagRange( \"{CollectionNamespace.FullName}\", {min}, {max}, \"{zoneName}\");";
				tagRangeCommands.AppendLine(addTagRangeCommand);
			}
			
			
			Console.WriteLine("//Split Commands:");
			Console.WriteLine(splitCommands);
			
			Console.WriteLine("//Tag Range Commands:");
			Console.WriteLine(tagRangeCommands);
		}
	}
}
