using CommandLine;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.ClusterMaintenance
{
	[Verb("scan", HelpText = "Scan chunks")]
	public class ScanChunks: BaseOptions
	{
		[Option("sizes", Separator = ',', Required = false, HelpText = "additional sizes of chunks")]
		public IList<string> Sizes { get; set; }

		public override async Task Run(CancellationToken token)
		{



			var collRepo = new CollectionRepository(MongoClient);
			var chunkRepo = new ChunkRepository(MongoClient);

			var db = MongoClient.GetDatabase(Database);
			
			var collInfo = await collRepo.Find(Database, Collection);

			if (collInfo == null)
				throw new InvalidOperationException($"collection {Database}.{Collection} not sharded");

			var scanner = new EmptyChunkScanner(db, collInfo, chunkRepo, ShardNames, Sizes, token);

			await scanner.Run();
		}
	}
}
