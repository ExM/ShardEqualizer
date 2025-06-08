using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ShardEqualizer.DAL.WorkLoad;

public class ItemModel
{
    public static ItemModel Create(int groupId) => new ItemModel()
    {
        Id = ObjectId.GenerateNewId(),
        GroupId = groupId,
        Payload = new byte[256]
    };
    
    [BsonId]
    public required ObjectId Id { get; init; }

    [BsonElement("group"), BsonRequired]
    public required int GroupId { get; init; }

    [BsonElement("payload"), BsonRequired]
    public required byte[] Payload { get; init; }
}