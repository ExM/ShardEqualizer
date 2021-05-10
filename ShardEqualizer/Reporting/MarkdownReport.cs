using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShardEqualizer.Reporting
{
	public class MarkdownReport: BaseReport
	{
		public MarkdownReport(SizeRenderer sizeRenderer) : base(sizeRenderer)
		{
		}

		protected override void AppendRow(StringBuilder sb, string rowTitle, params long?[] cells)
		{
			sb.Append("| **");
			sb.Append(rowTitle);
			sb.Append("** | ");
			sb.Append(string.Join(" | ", cells.Select(_ => _.HasValue ? $"`{SizeRenderer.Render(_.Value)}`": "")));
			sb.AppendLine(" |");
		}

		protected override void AppendHeader(StringBuilder sb, ICollection<string> cells)
		{
			sb.AppendLine();
			sb.Append("| ");
			sb.Append(string.Join(" | ", cells.Select(x => x.Replace("\r\n", "<br>"))));
			sb.AppendLine(" |");


			sb.Append("| - |");
			foreach (var alCell in Enumerable.Repeat(" -: |", cells.Count - 1))
				sb.Append(alCell);
			sb.AppendLine();
		}
	}
}
