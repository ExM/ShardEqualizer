using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace ShardEqualizer.DAL.Serialization;

public sealed class CollectionNamespaceSerializer : ClassSerializerBase<CollectionNamespace?>
{
	private static readonly CollectionNamespaceSerializer _instance = new ();
	
	public static void Register()
	{
		BsonSerializer.TryRegisterSerializer(typeof(CollectionNamespace), _instance);
	}
		
	private readonly StringSerializer _serializer = new ();

	public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, CollectionNamespace? value)
	{
		_serializer.Serialize(context, args, value?.FullName);
	}

	public override CollectionNamespace? Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
	{
		var text = _serializer.Deserialize(context, args);
		return text is null ? null : CollectionNamespace.FromFullName(text);
	}
}