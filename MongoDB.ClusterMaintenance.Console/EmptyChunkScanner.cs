using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NLog;

namespace MongoDB.ClusterMaintenance
{
	public class EmptyChunkScanner
	{
		private static readonly Logger _log = LogManager.GetCurrentClassLogger();
		
		private readonly IMongoDatabase _db;
		private readonly ShardedCollectionInfo _collInfo;
		private readonly ChunkRepository _chunkRepo;
		private readonly CancellationToken _token;

		private Stopwatch _sw = null;
		private long _totalChunks = 0;
		
		private readonly object _sync = new object();
		private long _processedChunks = 0;
		private readonly Dictionary<string, long> _zeroChunkCountByShards = new Dictionary<string, long>();

		public EmptyChunkScanner(IMongoDatabase db, ShardedCollectionInfo collInfo, ChunkRepository chunkRepo, CancellationToken token)
		{
			_db = db;
			_collInfo = collInfo;
			_chunkRepo = chunkRepo;
			_token = token;
		}

		public async Task Run()
		{
			_totalChunks = await _chunkRepo.Count(_collInfo.Id);
			_log.Info("Total chunks: {0}", _totalChunks);
			_sw = Stopwatch.StartNew();

			var task = showProgressLoop();
			var cursor = await _chunkRepo.Find(_collInfo.Id);
			await cursor.ForEachAsync(chunk => processChunk(chunk), _token);

			await task;
		}

		private async Task showProgressLoop()
		{
			while (showProgress())
				await Task.Delay(TimeSpan.FromSeconds(1), _token);
		}

		private bool showProgress()
		{
			Dictionary<string, long> copyZeroChunkCountByShards;
			long copyProcessedChunks;
			
			lock (_sync)
			{
				copyZeroChunkCountByShards = new Dictionary<string, long>(_zeroChunkCountByShards);
				copyProcessedChunks = _processedChunks;
			}

			var elapsed = _sw.Elapsed;
			var percent = (double)copyProcessedChunks / _totalChunks;
			var s = percent <= 0 ? 0 : (1 - percent) / percent;

			var eta = TimeSpan.FromSeconds(elapsed.TotalSeconds * s);

			_log.Info("Progress {0}/{1} Elapsed: {2} ETA: {3}", copyProcessedChunks, _totalChunks, elapsed, eta);
			var report = string.Join(", ", copyZeroChunkCountByShards.Select(_ => $"{_.Key}:{_.Value}"));
			_log.Info("Report: {0}", report);
			return copyProcessedChunks < _totalChunks;
		}

		private void incrementZeroChunk(string shard)
		{
			lock(_sync)
			{
				if (_zeroChunkCountByShards.TryGetValue(shard, out var count))
					_zeroChunkCountByShards[shard] = count + 1;
				else
					_zeroChunkCountByShards.Add(shard, 1);
			}
		}
		
		private async Task processChunk(ChunkInfo chunk)
		{
			_log.Debug("Process chunk: {0}/{1}", chunk.Id, chunk.Shard);

			var result = await runDatasizeCommand(chunk);

			if (result.IsSuccess)
			{
				_log.Debug("chunk: {0}/{1} size: {2}", chunk.Id, chunk.Shard, result.Size);
				
				if(result.Size == 0)
					incrementZeroChunk(chunk.Shard);
			}
			else
				_log.Warn("datasize command fail");

			lock (_sync)
				_processedChunks++;
		}
		
		private async Task<DatasizeResult> runDatasizeCommand(ChunkInfo chunk)
		{
			var cmd = new BsonDocument
			{
				{ "datasize", _collInfo.Id },
				{ "keyPattern", _collInfo.Key },
				{ "min", chunk.Min },
				{ "max", chunk.Max }
			};

			return await _db.RunCommandAsync<DatasizeResult>(cmd, null, _token);
		}
	}
}