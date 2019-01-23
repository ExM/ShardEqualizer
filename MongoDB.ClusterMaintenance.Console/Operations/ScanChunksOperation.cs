using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.ClusterMaintenance.Config;
using MongoDB.Driver;
using NLog;

namespace MongoDB.ClusterMaintenance.Operations
{
	public class ScanChunksOperation : IOperation
	{
		private static readonly Logger _log = LogManager.GetCurrentClassLogger();

		private readonly IReadOnlyList<Interval> _intervals;
		private readonly IConfigDbRepositoryProvider _configDb;
		private readonly IMongoClient _mongoClient;
		private readonly IList<string> _sizes;

		public ScanChunksOperation(IReadOnlyList<Interval> intervals, IConfigDbRepositoryProvider configDb, IMongoClient mongoClient, IList<string> sizes)
		{
			_intervals = intervals;
			_configDb = configDb;
			_mongoClient = mongoClient;
			_sizes = sizes;
		}

		public async Task Run(CancellationToken token)
		{
			foreach (var interval in _intervals)
			{
				_log.Info("Scan interval {0} {1}", interval.ChunkFrom, interval.ChunkTo);
				await scanInterval(interval, token);
			}
		}

		private async Task scanInterval(Interval interval, CancellationToken token)
		{
			var db = _mongoClient.GetDatabase(interval.Namespace.DatabaseNamespace.DatabaseName);

			var collInfo = await _configDb.Collections.Find(interval.Namespace);

			if (collInfo == null)
				throw new InvalidOperationException($"collection {interval.Namespace.FullName} not sharded");

			var filtered = _configDb.Chunks
				.ByNamespace(interval.Namespace)
				.ChunkFrom(interval.ChunkFrom)
				.ChunkTo(interval.ChunkTo);

			var sizesBounds = _sizes.Select(BinaryPrefix.Parse).ToList();
			var header = string.Join("; ",
				new string[] {"shard", "jumbo", "empty"}.Concat(_sizes).Concat(new string[] {"Max"}));
			var chunkCountByShards = new ConcurrentDictionary<string, ChunkCounts>();

			var progress = new ProgressReporter(await filtered.Count(), () =>
			{
				var report = new StringBuilder();
				report.AppendLine();
				report.AppendLine(header);

				foreach (var pair in chunkCountByShards.ToArray())
				{
					report.AppendFormat("{0}; {1}", pair.Key, pair.Value.Render());
					report.AppendLine();
				}

				_log.Info("Report: {0}", report);
			});

			await (await filtered.Find()).ForEachAsync(async chunk =>
			{
				_log.Debug("Process chunk: {0}/{1}", chunk.Id, chunk.Shard);

				var result = await db.Datasize(collInfo, chunk, token);

				if (result.IsSuccess)
				{
					_log.Debug("chunk: {0}/{1} size: {2}", chunk.Id, chunk.Shard, result.Size);

					chunkCountByShards
						.GetOrAdd(chunk.Shard, _ => new ChunkCounts(sizesBounds))
						.Increment(chunk.Jumbo ?? false, result.Size);
				}
				else
					_log.Warn("datasize command fail");

				progress.Increment();
			}, token);

			await progress.Finalize();
		}

		private class ChunkCounts
		{
			private long _jumbo;
			private long _empty;
			private long _max;
			private readonly IList<long> _sizesBounds;
			private readonly long[] _byBounds;

			public ChunkCounts(IList<long> sizesBounds)
			{
				_sizesBounds = sizesBounds;
				_byBounds = new long[sizesBounds.Count];
			}

			public string Render()
			{
				lock (this)
				{
					var cells = new long[] {_jumbo, _empty}.Concat(_byBounds).Concat(new long[] {_max});
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

				lock (this)
				{
					if (jumbo)
						_jumbo++;

					if (size == 0)
						_empty++;
					else if (index >= 0)
						_byBounds[index]++;
					else
						_max++;
				}
			}
		}
	}
}