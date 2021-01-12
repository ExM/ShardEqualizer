using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;

namespace MongoDB.ClusterMaintenance.Models
{
	[BsonSerializer(typeof(Serializer))]
	public struct TagIdentity: IEquatable<TagIdentity>, IComparable<TagIdentity>
	{
		public TagIdentity(string id)
		{
			_id = id ?? throw new ArgumentNullException(nameof(id));
		}

		private readonly string _id;

		public static explicit operator string(TagIdentity source)
		{
			return source._id;
		}

		public static bool operator ==(TagIdentity x, TagIdentity y)
		{
			return x.Equals(y);
		}

		public static bool operator !=(TagIdentity x, TagIdentity y)
		{
			return !(x == y);
		}

		public bool Equals(TagIdentity other)
		{
			return string.Equals(other._id, _id, StringComparison.Ordinal);
		}

		public override bool Equals(object obj)
		{
			return obj is TagIdentity a && Equals(a);
		}

		public override int GetHashCode()
		{
			return _id.GetHashCode();
		}

		public int CompareTo(TagIdentity other)
		{
			return string.Compare(_id, other._id, StringComparison.Ordinal);
		}

		public override string ToString()
		{
			return _id;
		}

		public static explicit operator TagIdentity(string id)
		{
			return new TagIdentity(id);
		}

		internal class Serializer : StructSerializerBase<TagIdentity>
		{
			public override TagIdentity Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
			{
				return new TagIdentity(context.Reader.ReadString());
			}

			public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TagIdentity value)
			{
				context.Writer.WriteString(value._id);
			}
		}
	}
}

