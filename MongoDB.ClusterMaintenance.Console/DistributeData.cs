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
	[Verb("dist", HelpText = "distribute data by zones")]
	public class DistributeData : BaseOptions
	{
		private static readonly Logger _log = LogManager.GetCurrentClassLogger();
		
		[Option("zones", Separator = ',', Required = true, HelpText = "Name of zones.")]
		public IList<string> Zones { get; set; }
		
		[Option("chunkFrom", Required = false, HelpText = "Chunk scan left boundary.")]
		public string ChunkFrom { get; set; }

		[Option("chunkTo", Required = false, HelpText = "Chunk scan right boundary.")]
		public string ChunkTo { get; set; }

		public override async Task Run(CancellationToken token)
		{
			var collRepo = new CollectionRepository(MongoClient);
			var collInfo = await collRepo.Find(CollectionNamespace);

			if (collInfo == null)
				throw new InvalidOperationException($"collection {Database}.{Collection} not sharded");

			var filtered = new ChunkRepository(MongoClient)
				.ByNamespace(CollectionNamespace)
				.ChunkFrom(ChunkFrom)
				.ChunkTo(ChunkTo);

			var chunks = await (await filtered.Find()).ToListAsync(token);

			var allCommands = new StringBuilder(); 
			
			foreach (var part in chunks.Split(Zones.Count).Select((items, order) => new { Items = items, Order = order } ))
			{
				var zoneName = Zones[part.Order];
				var min = part.Items.First().Min.ToJson(new JsonWriterSettings() { Indent = false});
				var max = part.Items.Last().Max.ToJson(new JsonWriterSettings() { Indent = false});
				_log.Info("Zone: {0} ({1}) from {2} to {3}", zoneName, part.Items.Count, min, max);
				
				var addTagRangeCommand = $"sh.addTagRange( \"{CollectionNamespace.FullName}\", {min}, {max}, \"{zoneName}\");";
				allCommands.AppendLine(addTagRangeCommand);
			}
			
			Console.WriteLine("//Tag Range Commands:");
			Console.WriteLine(allCommands);
			/*
			var progress = new ProgressReporter(await filtered.Count());

			await (await filtered.Find()).ForEachAsync(async chunk =>
			{
				_log.Info("Total chunks: {0}", chunk.Min.ToJson(new JsonWriterSettings() { Indent = false}));
				progress.Increment();
				
				
				
			}, token);

			await progress.Finalize();
			*/
		}
	}
}
