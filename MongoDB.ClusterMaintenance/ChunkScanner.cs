using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.ClusterMaintenance.Models;
using MongoDB.ClusterMaintenance.MongoCommands;
using MongoDB.Driver;
using NLog;

namespace MongoDB.ClusterMaintenance
{
	public abstract class ChunkScanner
	{
		private static readonly Logger _log = LogManager.GetCurrentClassLogger();
		
		private readonly IMongoDatabase _db;
		private readonly ChunkRepository.Filtered _chunkRepo;
		private readonly CollectionNamespace _ns;
		private readonly BsonDocument _key;
		private readonly CancellationToken _token;

		private Stopwatch _sw = null;
		private long _totalChunks = 0;

		private long _processedChunks = 0;

		public ChunkScanner(IMongoDatabase db, ChunkRepository chunkRepo,
			CollectionNamespace ns, BsonDocument key, CancellationToken token)
		{
			_db = db;
			_chunkRepo = chunkRepo.ByNamespace(ns);
			_ns = ns;
			_key = key;
			_token = token;
		}

		public async Task Run()
		{
			_totalChunks = await _chunkRepo.Count();

			_log.Info("Total chunks: {0}", _totalChunks);
			_sw = Stopwatch.StartNew();

			var task = showProgressLoop();
			var cursor = await _chunkRepo.Find();
			await cursor.ForEachAsync(chunk => processChunk(chunk), _token);

			Thread.VolatileWrite(ref _processedChunks, _totalChunks);
			await task;
		}

		private async Task showProgressLoop()
		{
			while (showProgress())
				await Task.Delay(TimeSpan.FromSeconds(1), _token);
		}

		protected abstract void ShowProgress();

		private bool showProgress()
		{
			var copyProcessedChunks = Thread.VolatileRead(ref _processedChunks);

			var elapsed = _sw.Elapsed;
			var percent = (double)copyProcessedChunks / _totalChunks;
			var s = percent <= 0 ? 0 : (1 - percent) / percent;

			var eta = TimeSpan.FromSeconds(elapsed.TotalSeconds * s);

			_log.Info("Progress {0}/{1} Elapsed: {2} ETA: {3}", copyProcessedChunks, _totalChunks, elapsed, eta);

			ShowProgress();

			return copyProcessedChunks < _totalChunks;
		}

		protected abstract void ProcessChunk(Chunk chunk, DatasizeResult datasize);

		private async Task processChunk(Chunk chunk)
		{
			_log.Debug("Process chunk: {0}/{1}", chunk.Id, chunk.Shard);

			ProcessChunk(chunk,
				await _db.Datasize(_ns, _key, chunk.Min, chunk.Max, _token));

			Interlocked.Increment(ref _processedChunks);
		}
	}
}