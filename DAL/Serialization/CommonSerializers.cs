using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace ShardEqualizer.DAL.Serialization;

public static class CommonSerializers
{
    public static void Register()
    {
        CollectionNamespaceSerializer.Register();
        BsonSerializer.TryRegisterSerializer(GuidSerializer.StandardInstance);
    }
}