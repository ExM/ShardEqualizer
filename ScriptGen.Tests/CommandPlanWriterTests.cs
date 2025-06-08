using System.Text;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver;
using ShardEqualizer.DAL.Models;
using Xunit;

namespace ShardEqualizer.ScriptGen;

public class CommandPlanWriterTests
{
	[Fact]
	public void Comment()
	{
		var lines = RenderAllLines(w => w.Comment("hello world"));
		
		Assert.Collection(lines,
			l => l.Should().StartWith("// machine: "),
			l => l.Should().StartWith("// date: "),
			l => l.Should().Be("// hello world"),
			l => l.Should().BeEmpty());
	}

	[Fact]
	public void MergeTagRangeCommands()
	{
		var lines = RenderCodeLines(w =>
		{
			using var buffer = new TagRangeCommandBuffer(w, CollectionNamespace.FromFullName("x.A"));
			buffer.RemoveTagRange(TestBound(10), TestBound(20), new TagIdentity("T1"));
			buffer.RemoveTagRange(TestBound(20), TestBound(30), new TagIdentity("T2"));
			buffer.AddTagRange(TestBound(10), TestBound(20), new TagIdentity("T1"));
		});
		
		lines.Should().ContainSingle().Subject
			.Should().Be("""sh.removeTagRange( "x.A", { "x" : NumberInt(20) }, { "x" : NumberInt(30) }, "T2");""");
	}

	[Fact]
	public void AddTagRangeCommands()
	{
		var ns = CollectionNamespace.FromFullName("x.A");
		
		var lines = RenderCodeLines(w => w.AddTagRange(ns, TestBound(10), TestBound(20), new TagIdentity("tagA")));

		lines.Should().ContainSingle().Subject
			.Should().Be("""sh.addTagRange( "x.A", { "x" : NumberInt(10) }, { "x" : NumberInt(20) }, "tagA");""");
	}
	
	[Fact]
	public void ClearJumboFlag()
	{
		var ns = CollectionNamespace.FromFullName("x.A");

		var lines = RenderCodeLines(w => w.ClearJumboFlag(ns, TestBound(10), TestBound(20)));

		lines.Should().ContainSingle().Subject
			.Should().Be("""db.adminCommand({ clearJumboFlag: "x.A", bounds: [ { "x" : NumberInt(10) }, { "x" : NumberInt(20) } ] });""");
	}
	
	private string[] RenderAllLines(Action<CommandPlanWriter> write)
	{
		var sb = new StringBuilder();
		using (var sw = new StringWriter(sb))
		{
			sw.NewLine = "\r\n";
			var writer = new CommandPlanWriter(sw);
			write(writer);
		}

		return sb.ToString().Split(["\r\n"], StringSplitOptions.None);
	}
	
	private string[] RenderCodeLines(Action<CommandPlanWriter> write)
	{
		return RenderAllLines(write)
			.Where(s => !string.IsNullOrWhiteSpace(s))
			.Skip(2) // header
			.ToArray();
	}

	private BsonBound TestBound(int x)
	{
		return (BsonBound)new BsonDocument("x", x);
	}
}