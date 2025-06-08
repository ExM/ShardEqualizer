using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using ShardEqualizer.ByteSizeRendering;
using ShardEqualizer.DAL.Models;
using ShardEqualizer.Reporting;
using ShardEqualizer.ShardedClusterViews.Models;

namespace ShardEqualizer
{
	[TestFixture]
	public class SizeGroupTests
	{
		[Test]
		public void Demo()
		{
			var report = new TestReport();

			report.Append(GetCollStat("A", 100), null);
			report.Append(GetCollStat("A", 100), false);
			report.Append(GetCollStat("A", 100), true);
			report.Append(GetCollStat("A", 100), true);

			report.Append(GetCollStat(new Dictionary<ShardIdentity, ShardCollectionStatistics>()
			{
				{new ShardIdentity("A"), GetCollStat(100) },
				{new ShardIdentity("B"), GetCollStat(10) },
			}), null);

			report.Append(GetCollStat(new Dictionary<ShardIdentity, ShardCollectionStatistics>()
			{
				{new ShardIdentity("A"), GetCollStat(100) },
				{new ShardIdentity("B"), GetCollStat(10) },
			}), false);

			report.Append(GetCollStat(new Dictionary<ShardIdentity, ShardCollectionStatistics>()
			{
				{new ShardIdentity("A"), GetCollStat(100) },
				{new ShardIdentity("B"), GetCollStat(10) },
			}), true);

			report.Append(GetCollStat( new Dictionary<ShardIdentity, ShardCollectionStatistics>()
			{
				{new ShardIdentity("A"), GetCollStat(100) },
				{new ShardIdentity("B"), GetCollStat(10) },
			}), true);

			report.Append(GetCollStat("B", 100), null);
			report.Append(GetCollStat("B", 100), false);
			report.Append(GetCollStat("B", 100), true);
			report.Append(GetCollStat("B", 100), true);

			report.Render(new [] { new ColumnDescription(DataType.Total, SizeType.DataSize, false)});


			Assert.That(report.FirstColumn["A"], Is.EqualTo(800));
			Assert.That(report.FirstColumn["B"], Is.EqualTo(440));
		}

		public class TestReport: BaseReport
		{
			public Dictionary<string, long> FirstColumn = new Dictionary<string, long>();

			public TestReport() : base(new SizeRenderer("", ScaleSuffix.None))
			{
			}

			protected override void AppendShardRow(StringBuilder sb, string rowTitle, params long?[] cells)
			{
				FirstColumn.Add(rowTitle, cells[0].Value);
			}

			protected override void AppendOverallRow(StringBuilder sb, string rowTitle, params long?[] cells)
			{
				FirstColumn.Add(rowTitle, cells[0].Value);
			}

			protected override void AppendHeader(StringBuilder sb, ICollection<string> cells)
			{
			}
		}

		private ShardCollectionStatistics GetCollStat(long size)
		{
			return new ShardCollectionStatistics()
			{
				Size = size,
				StorageSize = size,
				TotalIndexSize = 0
			};
		}
		
		private CollectionStatistics GetCollStat(string primary, long size)
		{
			return new CollectionStatistics()
			{
				Primary = new ShardIdentity(primary),
				Sharded = false,
				Size = size,
				Shards = new Dictionary<ShardIdentity, ShardCollectionStatistics>(),
				StorageSize = size,
				TotalIndexSize = 0
			};
		}
		
		private CollectionStatistics GetCollStat(Dictionary<ShardIdentity, ShardCollectionStatistics> shards)
		{
			return new CollectionStatistics()
			{
				Primary = null,
				Sharded = true,
				Size = 0,
				Shards = shards,
				StorageSize = 0,
				TotalIndexSize = 0
			};
		}
	}
}
