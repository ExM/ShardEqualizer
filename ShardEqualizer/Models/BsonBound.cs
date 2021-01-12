using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;

namespace MongoDB.ClusterMaintenance.Models
{
	[BsonSerializer(typeof(Serializer))]
	public struct BsonBound: IEquatable<BsonBound>, IComparable<BsonBound>
	{
		public BsonBound(BsonDocument id)
		{
			_value = id ?? throw new ArgumentNullException(nameof(id));
		}

		private readonly BsonDocument _value;

		public static explicit operator BsonDocument(BsonBound source)
		{
			return source._value;
		}

		public static bool operator ==(BsonBound x, BsonBound y)
		{
			return x.Equals(y);
		}

		public static bool operator !=(BsonBound x, BsonBound y)
		{
			return !(x == y);
		}

		public bool Equals(BsonBound other)
		{
			return other._value.Equals(_value);
		}

		public override bool Equals(object obj)
		{
			return obj is BsonBound a && Equals(a);
		}

		public override int GetHashCode()
		{
			return _value.GetHashCode();
		}

		public int CompareTo(BsonBound other)
		{
			return _value.CompareTo(other._value);
		}
		
		public static bool operator >(BsonBound x, BsonBound y)
		{
			return x.CompareTo(y) > 0;
		}

		public static bool operator <(BsonBound x, BsonBound y)
		{
			return x.CompareTo(y) < 0;
		}
		
		public static bool operator >=(BsonBound x, BsonBound y)
		{
			return x.CompareTo(y) >= 0;
		}

		public static bool operator <=(BsonBound x, BsonBound y)
		{
			return x.CompareTo(y) <= 0;
		}

		public override string ToString()
		{
			return _value.ToString();
		}

		public static explicit operator BsonBound(BsonDocument v)
		{
			return new BsonBound(v);
		}

		internal class Serializer : StructSerializerBase<BsonBound>
		{
			private static readonly BsonDocumentSerializer _serializer = new BsonDocumentSerializer();

			private static readonly BsonDeserializationArgs _deserializationArgs = 
				new BsonDeserializationArgs()
				{
					NominalType = typeof(BsonDocument)
				};

			private static readonly BsonSerializationArgs _serializationArgs =
				new BsonSerializationArgs()
				{
					NominalType = typeof(BsonDocument),
					SerializeIdFirst = true,
					SerializeAsNominalType = true
				};
			
			public override BsonBound Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
			{
				var doc = _serializer.Deserialize(context, _deserializationArgs);
				return new BsonBound(doc);
			}

			public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, BsonBound value)
			{
				_serializer.Serialize(context, _serializationArgs, value._value);
			}
		}
	}
}

