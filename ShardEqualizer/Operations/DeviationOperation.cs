using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ShardEqualizer.ByteSizeRendering;
using ShardEqualizer.ConfigServices;
using ShardEqualizer.Reporting;
using ShardEqualizer.Verbs;

namespace ShardEqualizer.Operations
{
	public class DeviationOperation: IOperation
	{
		private readonly CollectionListService _collectionListService;
		private readonly CollectionStatisticService _collectionStatisticService;
		private readonly IReadOnlyList<Interval> _intervals;
		private readonly ScaleSuffix _scaleSuffix;
		private readonly ReportFormat _reportFormat;
		private readonly List<LayoutDescription> _layouts;

		public DeviationOperation(
			CollectionListService collectionListService,
			CollectionStatisticService collectionStatisticService,
			IReadOnlyList<Interval> intervals,
			ScaleSuffix scaleSuffix,
			ReportFormat reportFormat,
			List<LayoutDescription> layouts)
		{
			_collectionListService = collectionListService;
			_collectionStatisticService = collectionStatisticService;
			_intervals = intervals;
			_scaleSuffix = scaleSuffix;
			_reportFormat = reportFormat;
			_layouts = layouts;
		}

		public async Task Run(CancellationToken token)
		{
			var userColls = await _collectionListService.Get(token);
			var allCollStats = await _collectionStatisticService.Get(userColls, token);

			var sizeRenderer = new SizeRenderer("F2", _scaleSuffix);

			var allShards = allCollStats.Values.SelectMany(x => x.Shards.Keys).Distinct().OrderBy(x => x.ToString(), StringComparer.Ordinal).ToList();

			var unShSizes = allCollStats.Where(x => !x.Value.Sharded).GroupBy(x => x.Value.Primary!.Value)
				.ToDictionary(x => x.Key, x => x.Sum(s => s.Value.Size));

			var minOtherSize = allCollStats.Where(x => x.Value.Sharded).Sum(x => x.Value.Size) / allShards.Count * 5 / 100;

			var orderedShardedColls =
				allCollStats.Where(x => x.Value.Sharded).OrderByDescending(x => x.Value.Size).ToList();

			var bigShardedColls = orderedShardedColls.Where(x => x.Value.Size >= minOtherSize);

			var otherSizes = orderedShardedColls.Where(x => x.Value.Size < minOtherSize)
				.SelectMany(x => x.Value.Shards).GroupBy(x => x.Key).ToDictionary(x => x.Key, x => x.Sum(s => s.Value.Size));

			Console.WriteLine($"DATA unsharded {string.Join(" ", bigShardedColls.Select(x => x.Key))} other");

			foreach (var shard in allShards)
			{
				var cells = new List<string>();

				cells.Add(unShSizes.TryGetValue(shard, out var size) ? sizeRenderer.Render(size) : "0");

				foreach (var pair in bigShardedColls)
				{
					cells.Add(sizeRenderer.Render(pair.Value.Shards[shard].Size));
				}

				cells.Add(sizeRenderer.Render(otherSizes[shard]));

				Console.WriteLine($"{shard} {string.Join(" ", cells)}");
			}


			/*
			var allShards = allCollStats.Values.SelectMany(x => x.Shards.Keys).Distinct().OrderBy(x => x.ToString(), StringComparer.Ordinal).ToList();

			Console.WriteLine($";{string.Join(";", allShards)}");

			foreach (var (ns, collStat) in allCollStats.Where(x => x.Value.Sharded))
			{
				var cells = allShards.Select(shard => collStat.Shards.TryGetValue(shard, out var statOnShard) ? sizeRenderer.Render(statOnShard.Size) : "0").ToList();

				Console.WriteLine($"{ns};{string.Join(";", cells)}");
			}

			foreach (var g in allCollStats.Where(x => !x.Value.Sharded).GroupBy(x => x.Key.DatabaseNamespace))
			{
				var nonSharded = g.Select(x => x.Value).GroupBy(x => x.Primary!.Value)
					.ToDictionary(x => x.Key, x => x.Sum(s => s.Size));
				var cells = allShards.Select(shard => nonSharded.TryGetValue(shard, out var size) ? sizeRenderer.Render(size) : "0").ToList();

				Console.WriteLine($"no sharded in {g.Key};{string.Join(";", cells)}");
			}
			*/
				/*
			var report = createReport(sizeRenderer);
			foreach (var (ns, collStats) in allCollStats)
			{
				var interval = _intervals.FirstOrDefault(_ => _.Namespace.FullName == ns.FullName);
				report.Append(collStats, interval?.Adjustable);
			}

			Console.WriteLine($"Report as {_reportFormat}:");
			Console.WriteLine();

			foreach (var layout in _layouts)
			{
				Console.WriteLine($"{layout.Title}:");
				Console.WriteLine(report.Render(layout.Columns));
				Console.WriteLine();
			}
			*/
		}

		private BaseReport createReport(SizeRenderer sizeRenderer)
		{
			switch (_reportFormat)
			{
				case ReportFormat.Csv:
					return new CsvReport(sizeRenderer);

				case ReportFormat.Markdown:
					return new MarkdownReport(sizeRenderer);

				default:
					throw new ArgumentException($"unexpected report format: {_reportFormat}");
			}
		}
	}
}
