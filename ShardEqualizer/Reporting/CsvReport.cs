using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShardEqualizer.Reporting
{
	public class CsvReport: BaseReport
	{
		public CsvReport(SizeRenderer sizeRenderer) : base(sizeRenderer)
		{
		}

		protected override void AppendRow(StringBuilder sb, string rowTitle, params long?[] cells)
		{
			sb.AppendLine(rowTitle +  ";" + string.Join(";", cells.Select(_ => _.HasValue ? SizeRenderer.Render(_.Value): "")));
		}

		protected override void AppendHeader(StringBuilder sb, ICollection<string> cells)
		{
			sb.AppendLine(string.Join(";", cells));
		}
	}
}
