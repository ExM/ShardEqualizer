using NUnit.Framework;
using ShardEqualizer.Reporting;

namespace ShardEqualizer
{
	[TestFixture]
	public class ColumnDescriptionTests
	{
		[TestCase("AjSz", SizeType.DataSize)]
		[TestCase("AjSt", SizeType.DataStorage)]
		[TestCase("AjIs", SizeType.IndexSize)]
		[TestCase("AjTs", SizeType.TotalStorage)]
		[TestCase("UmTs", SizeType.TotalStorage)]
		[TestCase("AjTsD", SizeType.TotalStorage)]
		public void SizeTypeParse(string text, SizeType expected)
		{
			Assert.That(ColumnDescription.Parse(text).SizeType, Is.EqualTo(expected));
		}

		[TestCase("AjSz", DataType.Adjustable)]
		[TestCase("FxSz", DataType.Fixed)]
		[TestCase("MnSz", DataType.Managed)]
		[TestCase("ShSz", DataType.Sharded)]
		[TestCase("TtSz", DataType.Total)]
		[TestCase("UmSz", DataType.UnManaged)]
		[TestCase("UsSz", DataType.UnSharded)]
		public void DataTypeParse(string text, DataType expected)
		{
			Assert.That(ColumnDescription.Parse(text).DataType, Is.EqualTo(expected));
		}
		
		[TestCase("UmSz", false)]
		[TestCase("UmSzD", true)]
		public void DeviationParse(string text, bool expected)
		{
			Assert.That(ColumnDescription.Parse(text).Deviation, Is.EqualTo(expected));
		}
	}
}