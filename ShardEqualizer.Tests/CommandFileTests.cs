using System;
using System.IO;
using System.Text;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using NUnit.Framework;
using ShardEqualizer.JsonSerialization;
using ShardEqualizer.MongoCommands;

namespace ShardEqualizer
{
	[TestFixture]
	public class CommandFileTests
	{
		[Test]
		public void SerializeBson()
		{
			var jsonWriterSettings = new JsonWriterSettings()
				{Indent = false, GuidRepresentation = GuidRepresentation.Unspecified};

			var point = new BsonDocument() {{"_id", ObjectId.Parse("800000000000000000000000")}};

			var point2 = new BsonDocument() {{"_id", Guid.Parse("FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF")}};

			var point3 = new BsonDocument() {{"field", 0}};

			var point4 = new BsonDocument() {{"field", 0.0}};

			var cmd =  new BsonDocument
			{
				{ "moveChunk", "demo.testCollection" },
				{ "find", point },
				{ "find2", point2 },
				{ "find3", point3 },
				{ "find4", point4 },
				{ "to", "a" }
			};

			var json = cmd.ToJson(jsonWriterSettings);

			var json1 = ShellJsonWriter.AsJson(cmd);

			var json2 = cmd.ToJson(new JsonWriterSettings()
				{Indent = false, OutputMode = JsonOutputMode.RelaxedExtendedJson});

			var jsonReaderSettings = new JsonReaderSettings()
				{GuidRepresentation = GuidRepresentation.Unspecified};

			var parsed = parse(json, jsonReaderSettings);

			Assert.AreEqual(cmd, parsed);
		}

		public static BsonDocument parse(string json, JsonReaderSettings jsonReaderSettings)
		{
			using (var jsonReader = new JsonReader(json, jsonReaderSettings))
			{
				var bsonDocument = BsonDocumentSerializer.Instance.Deserialize(BsonDeserializationContext.CreateRoot(jsonReader));
				if (!jsonReader.IsAtEndOfFile())
					throw new FormatException("String contains extra non-whitespace characters beyond the end of the document.");
				return bsonDocument;
			}
		}

		[Test]
		public void JsFormat()
		{
			var sb = new StringBuilder();
			using (var sw = new StringWriter(sb))
			{
				var writer = new CommandPlanWriter(sw);
				writer.Comment("hello world");
			}

			Console.WriteLine(sb);
		}
	}
}
