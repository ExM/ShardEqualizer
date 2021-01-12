using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace ShardEqualizer.Serialization
{
	public sealed class CollectionNamespaceSerializer : ClassSerializerBase<CollectionNamespace>
	{
		public static void Register()
		{
			BsonSerializer.RegisterSerializer(typeof(CollectionNamespace), new CollectionNamespaceSerializer());
		}
		
		private readonly StringSerializer _serializer = new StringSerializer();

		public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, CollectionNamespace value)
		{
			_serializer.Serialize(context, args, value?.FullName);
		}

		public override CollectionNamespace Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
		{
			var text = _serializer.Deserialize(context, args);
			return text == null ? null : CollectionNamespace.FromFullName(text);
		}
	}
}