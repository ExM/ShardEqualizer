using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.ClusterMaintenance.Config;
using MongoDB.ClusterMaintenance.MongoCommands;
using MongoDB.Driver;
using NLog;

namespace MongoDB.ClusterMaintenance.Operations
{
	public class MergeChunksOperation : IOperation
	{
		private static readonly Logger _log = LogManager.GetCurrentClassLogger();
			
		private readonly IReadOnlyList<Interval> _intervals;
		private readonly IConfigDbRepositoryProvider _configDb;
		private readonly IMongoClient _mongoClient;
		private readonly CommandPlanWriter _commandPlanWriter;

		public MergeChunksOperation(IConfigDbRepositoryProvider configDb, IReadOnlyList<Interval> intervals, IMongoClient mongoClient, CommandPlanWriter commandPlanWriter)
		{
			_configDb = configDb;
			_intervals = intervals;
			_mongoClient = mongoClient;
			_commandPlanWriter = commandPlanWriter;
		}

		public async Task Run(CancellationToken token)
		{
			foreach (var interval in _intervals)
			{
				_log.Info("Merge chunks from {0}", interval.Namespace.FullName);
				//UNDONE
				//_commandPlanWriter.DescriptionOnly($"Merge chunks from {interval.Namespace.FullName}");
				//await mergeChunks(interval, token);
			}
		}
		
		/*
		private async Task mergeChunks(Interval interval, CancellationToken token)
		{
			var collInfo = await _configDb.Collections.Find(interval.Namespace);
			var db = _mongoClient.GetDatabase(interval.Namespace.DatabaseNamespace.DatabaseName);

			var tags = await _configDb.Tags.Get(interval.Namespace);
			var zoneBounds = new HashSet<BsonDocument>(
				tags.SelectMany(_ => new[] {_.Min, _.Max}).Distinct());
			
			if (collInfo == null)
				throw new InvalidOperationException($"collection {interval.Namespace.FullName} not sharded");

			var filtered = _configDb.Chunks
				.ByNamespace(interval.Namespace)
				.From(interval.Min)
				.To(interval.Max);

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
			var leftId = firstChunk.Id;
			var leftMin = firstChunk.Min;

			//backward merge
			for (var i = 1; i < total; i++)
			{
				var candidate = chunks[i];

				if (zoneBounds.Contains(candidate.Min))
				{
					_log.Info("Skip chunk {0} - contains zone bound", candidate.Id, candidate.Shard);

					leftShard = candidate.Shard;
					leftId = candidate.Id;
					leftMin = candidate.Min;
				}
				else
				{
					var datasize = await db.Datasize(collInfo, candidate, token);
					if (datasize.Size == 0)
					{
						_log.Info("Found empty chunk {0} on {1}", candidate.Id, candidate.Shard);

						if (leftShard != candidate.Shard)
						{
							_log.Info("Move to {0}", leftShard);
							
							_commandPlanWriter
								.Description($"Move {candidate.Id} chunk to {leftShard} shard")
								.MoveChunk(collInfo.Id, candidate.Min, leftShard);
						}
						
						_commandPlanWriter
							.Description($"Merge {candidate.Id} chunk to {leftId} on {leftShard} shard ")
							.MergeChunks(collInfo.Id, leftMin, candidate.Max);

						merged++;
					}
					else
					{
						leftShard = candidate.Shard;
						leftId = candidate.Id;
						leftMin = candidate.Min;
					}
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
					
					_commandPlanWriter
						.Description($"Move {firstChunk.Id} chunk to {secondChunk.Shard} shard")
						.MoveChunk(collInfo.Id, firstChunk.Min, secondChunk.Shard);
				}

				_commandPlanWriter
					.Description($"Merge {firstChunk.Id} chunk to {secondChunk.Id} on {secondChunk.Shard} shard ")
					.MergeChunks(collInfo.Id, firstChunk.Min, secondChunk.Max);
			}
		}
		*/
	}
}