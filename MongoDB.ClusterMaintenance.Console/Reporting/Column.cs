using System.Linq;

namespace MongoDB.ClusterMaintenance.Reporting
{
	public class Column
	{
		public DataType DataType { get; set; }
		public SizeType SizeType { get; set;}
		public bool Deviation { get; set;}
		public long? Total => Deviation ? (long?)null : _total;
		public long? Average => Deviation ? (long?)null : _average;

		private readonly long[] _rows;
		private long _total = 0;
		private long _average = 0;

		public Column(int rows)
		{
			_rows = new long[rows];
		}

		public void SetRow(int index, ShardRow row)
		{
			_rows[index] = row.ByDataType(DataType).Sum(_ => _.BySizeType(SizeType));
		}

		public void CalcTotal()
		{
			_total = _rows.Sum();
			_average = _total / _rows.Length;
			
			if (Deviation)
			{
				for (var i = 0; i < _rows.Length; i++)
					_rows[i] -= _average;
			}
		}

		public long? GetRow(int index)
		{
			return _rows[index];
		}
	}
}