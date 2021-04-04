using NUnit.Framework;
using ShardEqualizer.ByteSizeRendering;

namespace ShardEqualizer
{
	[TestFixture]
	public class SizeRendererTests
	{
		[TestCase(0, "0 b")]
		[TestCase(-1, "-1 b")]
		[TestCase(1, "1 b")]
		[TestCase(999, "999 b")]
		[TestCase(1000, "0.98 Kb")]
		[TestCase(1010, "0.99 Kb")]
		[TestCase(1023, "1 Kb")]
		[TestCase(500, "500 b")]
		[TestCase(400, "400 b")]
		[TestCase(1024, "1 Kb")]
		[TestCase(1024 * 1023, "1 Mb")]
		[TestCase(1024 * 1024 - 1, "1 Mb")]
		[TestCase(1500000, "1.43 Mb")]
		[TestCase((long)(5.38 * 1024 * 1024 * 1024), "5.38 Gb")]
		[TestCase((long)(1024 * 1024 * 1024 - 1), "1 Gb")]
		[TestCase(-1000, "-0.98 Kb")]
		[TestCase(int.MaxValue, "2 Gb")]
		[TestCase(int.MinValue, "-2 Gb")]
		[TestCase(long.MaxValue, "8 Eb")]
		[TestCase(long.MinValue + 1, "-8 Eb")]
		public void Format_byte_size(long size, string expected)
		{
			Assert.AreEqual(expected, size.ByteSize());
		}
	}
}