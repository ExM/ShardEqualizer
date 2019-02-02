using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;

namespace MongoDB.ClusterMaintenance.Models
{
	[BsonSerializer(typeof(Serializer))]
	public struct ShardIdentity: IEquatable<ShardIdentity>, IComparable<ShardIdentity>
	{
		public ShardIdentity(string id)
		{
			_id = id ?? throw new ArgumentNullException(nameof(id));
		}

		private readonly string _id;

		public static explicit operator string(ShardIdentity source)
		{
			return source._id;
		}

		public static bool operator ==(ShardIdentity x, ShardIdentity y)
		{
			return x.Equals(y);
		}

		public static bool operator !=(ShardIdentity x, ShardIdentity y)
		{
			return !(x == y);
		}

		public bool Equals(ShardIdentity other)
		{
			return string.Equals(other._id, _id, StringComparison.Ordinal);
		}

		public override bool Equals(object obj)
		{
			return obj is ShardIdentity a && Equals(a);
		}

		public override int GetHashCode()
		{
			return _id.GetHashCode();
		}

		public int CompareTo(ShardIdentity other)
		{
			return string.Compare(_id, other._id, StringComparison.Ordinal);
		}

		public override string ToString()
		{
			return _id;
		}

		public static explicit operator ShardIdentity(string id)
		{
			return new ShardIdentity(id);
		}

		internal class Serializer : StructSerializerBase<ShardIdentity>
		{
			public override ShardIdentity Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
			{
				return new ShardIdentity(context.Reader.ReadString());
			}

			public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, ShardIdentity value)
			{
				context.Writer.WriteString(value._id);
			}
		}
	}
}

