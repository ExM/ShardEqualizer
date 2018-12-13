using CommandLine;
using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver;
using NLog;

namespace MongoDB.ClusterMaintenance
{
	[Verb("merge", HelpText = "Merge empty or small chunks")]
	public class MergeChunks: BaseOptions
	{
		private static readonly Logger _log = LogManager.GetCurrentClassLogger();
		
		public override async Task Run(CancellationToken token)
		{
			var collInfo = await ConfigDb.Collections.Find(CollectionNamespace);
			var db = MongoClient.GetDatabase(Database);

			if (collInfo == null)
				throw new InvalidOperationException($"collection {Database}.{Collection} not sharded");

			var filtered = ConfigDb.Chunks
				.ByNamespace(CollectionNamespace);

			var chunks = await (await filtered.Find()).ToListAsync(token);
			var total = chunks.Count;
			if (total <= 1)
			{
				_log.Info("Merge not accepted. Total chunks: {0}", total);
				return;
			}
			
			var merged = 0;
			
			var progress = new ProgressReporter(total - 1, () =>
			{
				var copy = merged;
				_log.Info("Merged: {0}", copy);
			});

			var firstChunk = chunks[0];
			var leftShard = firstChunk.Shard;
			var leftMin = firstChunk.Min;
			
			//backward merge
			for (var i = 1; i < total; i++)
			{
				var candidate = chunks[i];
				
				var datasize = await db.Datasize(collInfo, candidate, token);
				if (datasize.Size == 0)
				{
					_log.Info("Found empty chunk {0} on {1}", candidate.Id, candidate.Shard);

					if (leftShard != candidate.Shard)
					{
						_log.Info("Move to {0}", leftShard);
						await AdminDB.MoveChunk(collInfo.Id, candidate.Min, leftShard, token);
					}
					await AdminDB.MergeChunks(collInfo.Id, leftMin, candidate.Max, token);

					merged++;
				}
				else
				{
					leftShard = candidate.Shard;
					leftMin = candidate.Min;
				}

				progress.Increment();
			}
			
			await progress.Finalize();
			
			//check first chunk
			chunks = await (await filtered.Find()).ToListAsync(token);
			if(chunks.Count <= 1)
				return;
			
			firstChunk = chunks[0];
			
			var datasizeFirst = await db.Datasize(collInfo, firstChunk, token);
			if (datasizeFirst.Size == 0)
			{
				_log.Info("Found first empty chunk on {0}", firstChunk.Shard);
				
				var secondChunk = chunks[1];

				if (firstChunk.Shard != secondChunk.Shard)
				{
					_log.Info("Move to {0}", secondChunk.Shard);
					await AdminDB.MoveChunk(collInfo.Id, firstChunk.Min, secondChunk.Shard, token);
				}
				
				await AdminDB.MergeChunks(collInfo.Id, firstChunk.Min, secondChunk.Max, token);
			}
		}
	}
}
