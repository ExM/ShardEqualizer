using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ShardEqualizer.ByteSizeRendering;
using ShardEqualizer.ConfigServices;
using ShardEqualizer.Reporting;
using ShardEqualizer.ShardedClusterViews;
using ShardEqualizer.Verbs;

namespace ShardEqualizer.Operations
{
	public class DeviationOperation: IOperation
	{
		private readonly UserCollectionsView _userCollectionsView;
		private readonly CollectionStatisticView _collectionStatisticView;
		private readonly IReadOnlyList<Interval> _intervals;
		private readonly ScaleSuffix _scaleSuffix;
		private readonly ReportFormat _reportFormat;
		private readonly List<LayoutDescription> _layouts;

		public DeviationOperation(
			UserCollectionsView userCollectionsView,
			CollectionStatisticView collectionStatisticView,
			IReadOnlyList<Interval> intervals,
			ScaleSuffix scaleSuffix,
			ReportFormat reportFormat,
			List<LayoutDescription> layouts)
		{
			_userCollectionsView = userCollectionsView;
			_collectionStatisticView = collectionStatisticView;
			_intervals = intervals;
			_scaleSuffix = scaleSuffix;
			_reportFormat = reportFormat;
			_layouts = layouts;
		}

		public async Task Run(CancellationToken token)
		{
			var userColls = await _userCollectionsView.Get(token);
			var allCollStats = await _collectionStatisticView.Get(userColls, token);

			var sizeRenderer = new SizeRenderer("F2", _scaleSuffix);

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
				Console.WriteLine($"{layout.Title} (in {_scaleSuffix.Text()}b):");
				Console.WriteLine(report.Render(layout.Columns));
				Console.WriteLine();
			}
		}

		private BaseReport createReport(SizeRenderer sizeRenderer)
		{
			return _reportFormat switch
			{
				ReportFormat.Csv => new CsvReport(sizeRenderer),
				ReportFormat.Markdown => new MarkdownReport(sizeRenderer),
				_ => throw new ArgumentException($"unexpected report format: {_reportFormat}")
			};
		}
	}
}
