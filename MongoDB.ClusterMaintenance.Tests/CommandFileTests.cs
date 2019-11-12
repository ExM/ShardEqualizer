using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.ClusterMaintenance.Models;
using MongoDB.ClusterMaintenance.MongoCommands;
using MongoDB.Driver;
using NUnit.Framework;

namespace MongoDB.ClusterMaintenance
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
			
			var cmd =  new BsonDocument
			{
				{ "moveChunk", "demo.testCollection" },
				{ "find", point },
				{ "find2", point2 },
				{ "find3", point3 },
				{ "to", "a" }
			};

			var json = cmd.ToJson(jsonWriterSettings);

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