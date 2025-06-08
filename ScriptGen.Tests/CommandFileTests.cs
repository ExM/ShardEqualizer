using FluentAssertions;
using MongoDB.Bson;
using Xunit;

namespace ShardEqualizer.ScriptGen;

public class ShellJsonWriterTests
{
	[Fact]
	public void SerializeStandardGuid()
	{
		var doc = new BsonDocument() {{"key", new BsonBinaryData(Guid.Parse("00010203-0405-0607-0809-0A0B0C0D0E0F"), GuidRepresentation.Standard)}};

		var json = ShellJsonWriter.AsJson(doc);

		json.Should().Be("""{ "key" : UUID("00010203-0405-0607-0809-0a0b0c0d0e0f") }""");
	}
	
	[Fact]
	public void SerializeCSharpLegacyGuid()
	{
		var doc = new BsonDocument() {{"key", new BsonBinaryData(Guid.Parse("00010203-0405-0607-0809-0A0B0C0D0E0F"), GuidRepresentation.CSharpLegacy)}};

		var json = ShellJsonWriter.AsJson(doc);

		json.Should().Be("""{ "key" : CSUUID("00010203-0405-0607-0809-0a0b0c0d0e0f") }""");
	}
	
	[Theory]
	[InlineData(0, "0")]
	[InlineData(1, "1")]
	[InlineData(-1, "-1")]
	[InlineData(3000000000, "3000000000")]
	[InlineData(-3000000000, "-3000000000")]
	public void SerializeLong(long value, string expected)
	{
		var doc = new BsonDocument("x", value);

		var json = ShellJsonWriter.AsJson(doc);

		json.Should().Be($$"""{ "x" : NumberLong("{{expected}}") }""");
	}
}