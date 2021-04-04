using System;
using System.Collections.Generic;
using System.Linq;
using ShardEqualizer.Config;

namespace ShardEqualizer.Reporting
{
	public class LayoutDescription
	{
		public string Title { get; }

		public IReadOnlyList<ColumnDescription> Columns { get; }

		public LayoutDescription(LayoutConfig config): this(config.Title, config.Columns)
		{
		}

		public LayoutDescription(string title, string columnsString)
		{
			Title = title;

			Columns = columnsString
				.Split(new char[] {' '}, StringSplitOptions.RemoveEmptyEntries)
				.Select(ColumnDescription.Parse)
				.ToList();
		}
	}
}
