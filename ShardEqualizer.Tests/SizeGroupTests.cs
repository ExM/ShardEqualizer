using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using ShardEqualizer.Config;
using ShardEqualizer.Models;
using ShardEqualizer.MongoCommands;
using ShardEqualizer.Reporting;

namespace ShardEqualizer
{
	[TestFixture]
	public class SizeGroupTests
	{
		[Test]
		public void Demo()
		{
			var report = new TestReport();

			report.Append(new CollStatsResult(){Primary = new ShardIdentity("A"), Sharded  = false, Size = 100}, null);
			report.Append(new CollStatsResult(){Primary = new ShardIdentity("A"), Sharded  = false, Size = 100}, CorrectionMode.None);
			report.Append(new CollStatsResult(){Primary = new ShardIdentity("A"), Sharded  = false, Size = 100}, CorrectionMode.Self);
			report.Append(new CollStatsResult(){Primary = new ShardIdentity("A"), Sharded  = false, Size = 100}, CorrectionMode.UnShard);

			report.Append(new CollStatsResult(){Sharded  = true, Shards = new Dictionary<ShardIdentity, CollStats>()
			{
				{new ShardIdentity("A"), new CollStats() { Size = 100} },
				{new ShardIdentity("B"), new CollStats() { Size = 10} },
			}}, null);

			report.Append(new CollStatsResult(){Sharded  = true, Shards = new Dictionary<ShardIdentity, CollStats>()
			{
				{new ShardIdentity("A"), new CollStats() { Size = 100} },
				{new ShardIdentity("B"), new CollStats() { Size = 10} },
			}}, CorrectionMode.None);

			report.Append(new CollStatsResult(){Sharded  = true, Shards = new Dictionary<ShardIdentity, CollStats>()
			{
				{new ShardIdentity("A"), new CollStats() { Size = 100} },
				{new ShardIdentity("B"), new CollStats() { Size = 10} },
			}}, CorrectionMode.Self);

			report.Append(new CollStatsResult(){Sharded  = true, Shards = new Dictionary<ShardIdentity, CollStats>()
			{
				{new ShardIdentity("A"), new CollStats() { Size = 100} },
				{new ShardIdentity("B"), new CollStats() { Size = 10} },
			}}, CorrectionMode.UnShard);

			report.Append(new CollStatsResult(){Primary = new ShardIdentity("B"), Sharded  = false, Size = 100}, null);
			report.Append(new CollStatsResult(){Primary = new ShardIdentity("B"), Sharded  = false, Size = 100}, CorrectionMode.None);
			report.Append(new CollStatsResult(){Primary = new ShardIdentity("B"), Sharded  = false, Size = 100}, CorrectionMode.Self);
			report.Append(new CollStatsResult(){Primary = new ShardIdentity("B"), Sharded  = false, Size = 100}, CorrectionMode.UnShard);

			report.Render(new [] { new ColumnDescription(DataType.Total, SizeType.DataSize, false)});


			Assert.AreEqual(800, report.FirstColumn["A"]);
			Assert.AreEqual(440, report.FirstColumn["B"]);
		}

		public class TestReport: BaseReport
		{
			public Dictionary<string, long> FirstColumn = new Dictionary<string, long>();

			public TestReport() : base(new SizeRenderer("", ScaleSuffix.None))
			{
			}

			protected override void AppendRow(StringBuilder sb, string rowTitle, params long?[] cells)
			{
				FirstColumn.Add(rowTitle, cells[0].Value);
			}

			protected override void AppendHeader(StringBuilder sb, IEnumerable<string> cells)
			{
				//throw new System.NotImplementedException();
			}
		}
	}
}
