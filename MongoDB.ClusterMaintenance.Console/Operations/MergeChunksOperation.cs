using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.ClusterMaintenance.Config;
using MongoDB.Driver;
using NLog;

namespace MongoDB.ClusterMaintenance.Operations
{
	public class MergeChunksOperation : IOperation
	{
		private static readonly Logger _log = LogManager.GetCurrentClassLogger();
			
		private readonly IReadOnlyList<Interval> _intervals;
		private readonly IConfigDbRepositoryProvider _configDb;
		private readonly IAdminDB _adminDb;
		private readonly IMongoClient _mongoClient;

		public MergeChunksOperation(IAdminDB adminDb, IConfigDbRepositoryProvider configDb, IReadOnlyList<Interval> intervals, IMongoClient mongoClient)
		{
			_adminDb = adminDb;
			_configDb = configDb;
			_intervals = intervals;
			_mongoClient = mongoClient;
		}

		public async Task Run(CancellationToken token)
		{
			foreach (var interval in _intervals)
			{
				_log.Info("Merge chunks from {0}", interval.Namespace.FullName);
				await mergeChunks(interval, token);
			}
		}

		private async Task mergeChunks(Interval interval, CancellationToken token)
		{
			//UNDONE use chunk bounds and current zones
			var collInfo = await _configDb.Collections.Find(interval.Namespace);
			var db = _mongoClient.GetDatabase(interval.Namespace.DatabaseNamespace.DatabaseName);

			if (collInfo == null)
				throw new InvalidOperationException($"collection {interval.Namespace.FullName} not sharded");

			var filtered = _configDb.Chunks
				.ByNamespace(interval.Namespace);

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
						await _adminDb.MoveChunk(collInfo.Id, candidate.Min, leftShard, token);
					}

					await _adminDb.MergeChunks(collInfo.Id, leftMin, candidate.Max, token);

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
			if (chunks.Count <= 1)
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
					await _adminDb.MoveChunk(collInfo.Id, firstChunk.Min, secondChunk.Shard, token);
				}

				await _adminDb.MergeChunks(collInfo.Id, firstChunk.Min, secondChunk.Max, token);
			}
		}
	}
}