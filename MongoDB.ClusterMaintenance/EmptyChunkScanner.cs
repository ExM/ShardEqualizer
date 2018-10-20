using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
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
		private readonly IList<string> _shardNames;
		private readonly IList<string> _sizesText;
		private readonly IList<long> _sizesBounds;
		private readonly CancellationToken _token;

		private Stopwatch _sw = null;
		private long _totalChunks = 0;
		
		private readonly object _sync = new object();
		private long _processedChunks = 0;
		private readonly ConcurrentDictionary<string, ChunkCounts> _chunkCountByShards = new ConcurrentDictionary<string, ChunkCounts>();

		private string _header;

		public EmptyChunkScanner(IMongoDatabase db, ShardedCollectionInfo collInfo, ChunkRepository chunkRepo,
			IList<string> shardNames, IList<string> sizes, CancellationToken token)
		{
			_db = db;
			_collInfo = collInfo;
			_chunkRepo = chunkRepo;
			_shardNames = shardNames;
			_sizesText = sizes;
			_sizesBounds = sizes.Select(BinaryPrefix.Parse).ToList();
			_token = token;

			_header = string.Join("; ", new string[] { "shard", "jumbo", "empty" }.Concat(_sizesText).Concat(new string[] { "Max " }));
		}

		public async Task Run()
		{
			_totalChunks = await _chunkRepo.Count(_collInfo.Id, _shardNames);

			_log.Info("Total chunks: {0}", _totalChunks);
			_sw = Stopwatch.StartNew();

			var task = showProgressLoop();
			var cursor = await _chunkRepo.Find(_collInfo.Id, _shardNames);
			await cursor.ForEachAsync(chunk => processChunk(chunk), _token);

			lock (_sync)
				_processedChunks = _totalChunks;
			
			await task;
		}

		private async Task showProgressLoop()
		{
			while (showProgress())
				await Task.Delay(TimeSpan.FromSeconds(1), _token);
		}

		private bool showProgress()
		{
			KeyValuePair<string, ChunkCounts>[] copyChunkCountByShards;
			long copyProcessedChunks;
			
			lock (_sync)
			{
				copyChunkCountByShards = _chunkCountByShards.ToArray();
				copyProcessedChunks = _processedChunks;
			}

			var elapsed = _sw.Elapsed;
			var percent = (double)copyProcessedChunks / _totalChunks;
			var s = percent <= 0 ? 0 : (1 - percent) / percent;

			var eta = TimeSpan.FromSeconds(elapsed.TotalSeconds * s);

			_log.Info("Progress {0}/{1} Elapsed: {2} ETA: {3}", copyProcessedChunks, _totalChunks, elapsed, eta);

			var report = new StringBuilder();
			report.AppendLine();
			report.AppendLine(_header);

			foreach (var pair in copyChunkCountByShards)
			{
				report.AppendFormat("{0}; {1}", pair.Key, pair.Value.Render());
				report.AppendLine();
			}

			_log.Info("Report: {0}", report);
			return copyProcessedChunks < _totalChunks;
		}
		
		private async Task processChunk(ChunkInfo chunk)
		{
			_log.Debug("Process chunk: {0}/{1}", chunk.Id, chunk.Shard);

			var result = await runDatasizeCommand(chunk);

			if (result.IsSuccess)
			{
				_log.Debug("chunk: {0}/{1} size: {2}", chunk.Id, chunk.Shard, result.Size);

				_chunkCountByShards
					.GetOrAdd(chunk.Shard, _ => new ChunkCounts(_sizesBounds))
					.Increment(chunk.Jumbo ?? false, result.Size);
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

		public class ChunkCounts
		{
			private long Jumbo;
			private long Empty;
			private long Max;
			private IList<long> _sizesBounds;
			private long[] ByBounds;

			public ChunkCounts(IList<long> sizesBounds)
			{
				_sizesBounds = sizesBounds;
				ByBounds = new long[sizesBounds.Count];
			}

			public string Render()
			{
				lock (this)
				{
					var cells = new long[] { Jumbo, Empty }.Concat(ByBounds).Concat(new long[] { Max });
					return string.Join("; ", cells.Select(_ => _.ToString()));
				}
			}

			public void Increment(bool jumbo, long size)
			{
				var index = -1;
				if (0 < size)
				{
					var pos = 0;
					foreach (var bound in _sizesBounds)
					{
						if (size < bound)
						{
							index = pos;
							break;
						}
						pos++;
					}
				}

				lock(this)
				{
					if (jumbo)
						Jumbo++;

					if (size == 0)
						Empty++;
					else if (index >= 0)
						ByBounds[index]++;
					else
						Max++;
				}
			}
		}
	}
}