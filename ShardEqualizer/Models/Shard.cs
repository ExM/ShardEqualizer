using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson.Serialization.Attributes;

namespace ShardEqualizer.Models
{
	public class Shard
	{
		private static readonly IReadOnlyList<TagIdentity> _emptyTags = new TagIdentity[0];

		public Shard()
		{
		}

		public Shard(string id, params string[] tags)
		{
			Id = new ShardIdentity(id);
			_tags = tags.Select( _ => new TagIdentity(_)).ToList();
		}

		[BsonId]
		public ShardIdentity Id { get; private set; }

		[BsonElement("host"), BsonRequired]
		public string Host { get; private set; }

		[BsonElement("state"), BsonRequired]
		public ShardState State { get; private set; }

		[BsonElement("tags"), BsonIgnoreIfNull]
		private IReadOnlyList<TagIdentity> _tags { get; set; }

		[BsonIgnore]
		public IReadOnlyList<TagIdentity> Tags => _tags ?? _emptyTags;

		[BsonElement("maxSize"), BsonIgnoreIfNull]
		public double? MaxSize { get; private set; }
	}
}
