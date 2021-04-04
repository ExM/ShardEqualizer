using System;
using System.Collections.Generic;

namespace ShardEqualizer.Reporting
{
	public class ShardRow
	{
		private readonly BaseReport _baseReport;

		public ShardRow(BaseReport baseReport)
		{
			_baseReport = baseReport;
		}

		public readonly SizeDetails UnSharded = new SizeDetails();
		public readonly SizeDetails UnManaged = new SizeDetails();
		public readonly SizeDetails Fixed = new SizeDetails();
		public readonly SizeDetails Adjustable = new SizeDetails();

		public IEnumerable<SizeDetails> ByDataType(DataType dataType)
		{
			switch (dataType)
			{
				case DataType.Adjustable: return new[] { Adjustable };
				case DataType.UnSharded: return new[] { UnSharded };
				case DataType.UnManaged: return new[] { UnManaged };
				case DataType.Fixed:  return new[] { Fixed };
				case DataType.Managed:  return new[] { UnSharded, Adjustable };
				case DataType.Sharded:  return new[] { UnManaged, Fixed, Adjustable };
				case DataType.Total: return new[] { UnSharded, UnManaged, Fixed, Adjustable};
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}
}