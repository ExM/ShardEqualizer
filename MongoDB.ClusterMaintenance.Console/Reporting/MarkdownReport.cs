using System.Linq;
using System.Text;

namespace MongoDB.ClusterMaintenance.Reporting
{
	public class MarkdownReport: BaseReport
	{
		public MarkdownReport(SizeRenderer sizeRenderer) : base(sizeRenderer)
		{
		}

		protected override void AppendRow(StringBuilder sb, string rowTitle, params long?[] cells)
		{
			sb.Append("|| ");
			sb.Append(rowTitle);
			sb.Append(" || ");
			sb.AppendLine(string.Join(" | ", cells.Select(_ => _.HasValue ? SizeRenderer.Render(_.Value): "")));
			sb.AppendLine(" |");
		}

		protected override void AppendHeader(StringBuilder sb, params string[] cells)
		{
			sb.Append("|| ");
			sb.Append(string.Join(" || ", cells));
			sb.AppendLine(" ||");
		}
	}
}