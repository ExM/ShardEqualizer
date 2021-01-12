using System;
using System.Globalization;

namespace MongoDB.ClusterMaintenance.Reporting
{
	public class ColumnDescription
	{
		public ColumnDescription(DataType dataType, SizeType sizeType, bool deviation)
		{
			DataType = dataType;
			SizeType = sizeType;
			Deviation = deviation;
		}

		public static ColumnDescription Parse(string text)
		{
			if(text.Length < 4 || 5 < text.Length)
				throw new FormatException("column description must contain from 4 to 5 characters");

			var dataType = toDataType(text.Substring(0, 2));
			var sizeType = toSizeType(text.Substring(2, 2));

			var deviation = false;
			if (text.Length == 5)
			{
				if(char.ToUpper(text[4], CultureInfo.InvariantCulture) != 'D')
					throw new FormatException($"unknown 5th symbol '{text[4]}'");
				deviation = true;
			} 
			return new ColumnDescription(dataType, sizeType, deviation);
		}

		private static SizeType toSizeType(string text)
		{
			switch (text.ToUpperInvariant())
			{
				case "SZ": return SizeType.DataSize;
				case "ST": return SizeType.DataStorage;
				case "IS": return SizeType.IndexSize;
				case "TS": return SizeType.TotalStorage;
				
				default:
					throw new FormatException($"unknown size type '{text}'");
			}
		}

		private static DataType toDataType(string text)
		{
			switch (text.ToUpperInvariant())
			{
				case "AJ": return DataType.Adjustable;
				case "FX": return DataType.Fixed;
				case "MN": return DataType.Managed;
				case "SH": return DataType.Sharded;
				case "TT": return DataType.Total;
				case "UM": return DataType.UnManaged;
				case "US": return DataType.UnSharded;
				
				default:
					throw new FormatException($"unknown data type '{text}'");
			}
		}

		public DataType DataType { get; }
		public SizeType SizeType { get; }
		public bool Deviation { get; }

		public string DataTypeHeader()
		{
			switch (DataType)
			{
				case DataType.UnSharded: return "UnShrd";
				case DataType.UnManaged: return "UnMan";
				case DataType.Fixed: return "Fixed";
				case DataType.Adjustable: return "Adj";
				case DataType.Sharded: return "Sharded";
				case DataType.Managed: return "Managed";
				case DataType.Total: return "Total";

				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public Column CreateColumn(int rows)
		{
			return new Column(rows)
			{
				DataType = DataType,
				SizeType = SizeType,
				Deviation = Deviation
			};
		}
		
		public string SizeTypeHeader()
		{
			switch (SizeType)
			{
				case SizeType.DataSize: return "DSize";
				case SizeType.DataStorage: return "DStore";
				case SizeType.IndexSize: return "Index";
				case SizeType.TotalStorage: return "TStore";
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public string DeviationHeader()
		{
			return Deviation ? "Delta" : "";
		}
		
		public string Header()
		{
			var result = $"{DataTypeHeader()} {SizeTypeHeader()}";
			if (Deviation)
				result += " " + DeviationHeader();
			return result;
		}
	}
}