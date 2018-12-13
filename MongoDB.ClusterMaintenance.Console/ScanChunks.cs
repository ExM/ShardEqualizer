using CommandLine;
using MongoDB.Driver;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using NLog;

namespace MongoDB.ClusterMaintenance
{
	[Verb("scan", HelpText = "Scan chunks")]
	public class ScanChunks: BaseOptions
	{
		private static readonly Logger _log = LogManager.GetCurrentClassLogger();
		
		[Option("sizes", Separator = ',', Required = false, HelpText = "additional sizes of chunks")]
		public IList<string> Sizes { get; set; }

		public override async Task Run(CancellationToken token)
		{
			var db = MongoClient.GetDatabase(Database);
			
			var collInfo = await ConfigDb.Collections.Find(CollectionNamespace);

			if (collInfo == null)
				throw new InvalidOperationException($"collection {Database}.{Collection} not sharded");

			var filtered = ConfigDb.Chunks
				.ByNamespace(CollectionNamespace)
				.ByShards(ShardNames);
			
			var sizesBounds = Sizes.Select(BinaryPrefix.Parse).ToList();
			var header = string.Join("; ", new string[] { "shard", "jumbo", "empty" }.Concat(Sizes).Concat(new string[] { "Max" }));
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
					var cells = new long[] { _jumbo, _empty }.Concat(_byBounds).Concat(new long[] { _max });
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
