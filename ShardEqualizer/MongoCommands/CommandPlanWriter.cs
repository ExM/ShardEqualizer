using System;
using System.IO;
using MongoDB.Bson;
using MongoDB.Driver;
using ShardEqualizer.JsonSerialization;
using ShardEqualizer.Models;

namespace ShardEqualizer.MongoCommands
{
	public class CommandPlanWriter
	{
		private readonly TextWriter _writer;

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
			var minText = ShellJsonWriter.AsJson((BsonDocument)min);
			var maxText = ShellJsonWriter.AsJson((BsonDocument)max);
			_writer.WriteLine($"sh.addTagRange( \"{collection.FullName}\", {minText}, {maxText}, \"{tag}\");");
		}

		public void RemoveTagRange(CollectionNamespace collection, BsonBound min, BsonBound max, TagIdentity tag)
		{
			var minText = ShellJsonWriter.AsJson((BsonDocument)min);
			var maxText = ShellJsonWriter.AsJson((BsonDocument)max);
			_writer.WriteLine($"sh.removeTagRange( \"{collection.FullName}\", {minText}, {maxText}, \"{tag}\");");
		}

		public void MergeChunks(CollectionNamespace ns, BsonBound min, BsonBound max)
		{
			var minText = ShellJsonWriter.AsJson((BsonDocument)min);
			var maxText = ShellJsonWriter.AsJson((BsonDocument)max);
			_writer.WriteLine($"db.adminCommand({{ mergeChunks: \"{ns.FullName}\", bounds: [ {minText}, {maxText} ] }});");
		}

		public void AddShardToZone(ShardIdentity shard, string zoneName)
		{
			_writer.WriteLine($"sh.addShardToZone(\"{shard}\", \"{zoneName}\");");
		}

		public void Flush()
		{
			_writer.Flush();
		}
	}
}
