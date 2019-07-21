using System;
using System.Globalization;
using System.IO;
using CsvHelper;
using CsvHelper.Configuration;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.ClusterMaintenance.Models;
using MongoDB.Driver;

namespace MongoDB.ClusterMaintenance.MongoCommands
{
	public class CommandPlanWriter
	{
		private readonly TextWriter _writer;

		private static readonly JsonWriterSettings _jsonWriterSettings = new JsonWriterSettings()
			{Indent = false, GuidRepresentation = GuidRepresentation.Unspecified};
		
		public CommandPlanWriter(TextWriter writer)
		{
			_writer = writer;
			Comment($"machine: {Environment.MachineName}");
			Comment($"date: {DateTime.UtcNow:u}");
		}

		public void Comment(string text)
		{
			_writer.Write("// ");
			_writer.WriteLine(text);
		}

		public void AddTagRange(CollectionNamespace collection, BsonBound min, BsonBound max, TagIdentity tag) 
		{
			var minText = ((BsonDocument)min).ToJson(_jsonWriterSettings);
			var maxText = ((BsonDocument)max).ToJson(_jsonWriterSettings);
			_writer.WriteLine($"sh.addTagRange( \"{collection.FullName}\", {minText}, {maxText}, \"{tag}\");");
		}
		
		public void RemoveTagRange(CollectionNamespace collection, BsonBound min, BsonBound max, TagIdentity tag) 
		{
			var minText = ((BsonDocument)min).ToJson(_jsonWriterSettings);
			var maxText = ((BsonDocument)max).ToJson(_jsonWriterSettings);
			_writer.WriteLine($"sh.removeTagRange( \"{collection.FullName}\", {minText}, {maxText}, \"{tag}\");");
		}
		
		public void Flush()
		{
			_writer.Flush();
		}
	}
}