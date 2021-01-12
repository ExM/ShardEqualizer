using System;
using System.IO;
using System.Text;
using MongoDB.Bson;
using MongoDB.Driver;
using NUnit.Framework;
using ShardEqualizer.Models;
using ShardEqualizer.MongoCommands;

namespace ShardEqualizer
{
	[TestFixture]
	public class CommandPlanWriterTests
	{
		[Test]
		public void Comment()
		{
			var sb = new StringBuilder();
			using (var sw = new StringWriter(sb))
			{
				var writer = new CommandPlanWriter(sw);
				writer.Comment("hello world");
			}

			var lines = sb.ToString().Split(new []{"\r\n"}, StringSplitOptions.None);

			Assert.AreEqual(@"// hello world", lines[2]);
		}

		[Test]
		public void MergeTagRangeCommands()
		{
			var sb = new StringBuilder();
			using (var sw = new StringWriter(sb))
			{
				var writer = new CommandPlanWriter(sw);

				using (var buffer = new TagRangeCommandBuffer(writer, CollectionNamespace.FromFullName("x.A")))
				{
					buffer.RemoveTagRange(testBound(10), testBound(20), new TagIdentity("T1"));
					buffer.RemoveTagRange(testBound(20), testBound(30), new TagIdentity("T2"));
					buffer.AddTagRange(testBound(10), testBound(20), new TagIdentity("T1"));
				}
			}

			var lines = sb.ToString().Split(new []{"\r\n"}, StringSplitOptions.None);

			Assert.AreEqual("sh.removeTagRange( \"x.A\", { \"x\" : NumberInt(20) }, { \"x\" : NumberInt(30) }, \"T2\");", lines[2]);
		}

		private BsonBound testBound(int x)
		{
			return (BsonBound)new BsonDocument("x", x);
		}
	}
}
