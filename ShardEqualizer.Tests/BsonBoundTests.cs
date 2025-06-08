using System;
using System.Linq;
using MongoDB.Bson;
using NUnit.Framework;
using ShardEqualizer.DAL.Models;

namespace ShardEqualizer
{
	[TestFixture]
	public class BsonBoundTests
	{
		[Test]
		public void ParseElementNames()
		{
			var bound = BsonBound.Parse("{ \"_id\" : NumberInt(10), \"n\": \"text\" }");

			var elements = ((BsonDocument) bound).Elements.Select(_ => _.Name).ToList();

			Assert.That(elements, Is.EquivalentTo(new [] {"_id", "n"}));
		}

		[TestCase("00000000-0000-0000-0000-000000000000")]
		[TestCase("22345200-abe8-4f60-90c8-0d43c5f6c0f6")]
		[TestCase("ffffffff-ffff-ffff-ffff-ffffffffffff")]
		public void ParseCSharpGuid(string text)
		{
			var bound = BsonBound.Parse($"{{ \"_id\" : CSUUID(\"{text}\") }}");

			var element = ((BsonDocument) bound).Elements.Single();

			Assert.That(element.Value, Is.InstanceOf<BsonBinaryData>());

			var bd = (BsonBinaryData)element.Value;

			Assert.That(bd.ToGuid(GuidRepresentation.CSharpLegacy), Is.EqualTo(Guid.Parse(text)));
		}
		
		[TestCase("00000000-0000-0000-0000-000000000000")]
		[TestCase("22345200-abe8-4f60-90c8-0d43c5f6c0f6")]
		[TestCase("ffffffff-ffff-ffff-ffff-ffffffffffff")]
		public void ParseStandardGuid(string text)
		{
			var bound = BsonBound.Parse($"{{ \"_id\" : UUID(\"{text}\") }}");

			var element = ((BsonDocument) bound).Elements.Single();

			Assert.That(element.Value, Is.InstanceOf<BsonBinaryData>());

			var bd = (BsonBinaryData)element.Value;

			Assert.That(bd.ToGuid(GuidRepresentation.Standard), Is.EqualTo(Guid.Parse(text)));
		}

		[Test]
		public void ParseMinKey()
		{
			var bound = BsonBound.Parse($"{{ \"_id\" : MinKey }}");

			var element = ((BsonDocument) bound).Elements.Single();

			Assert.That(element.Value.IsBsonMinKey, Is.True);
		}

		[Test]
		public void ParseMaxKey()
		{
			var bound = BsonBound.Parse($"{{ \"_id\" : MaxKey }}");

			var element = ((BsonDocument) bound).Elements.Single();

			Assert.That(element.Value.IsBsonMaxKey, Is.True);
		}

		[TestCase("000000000000000000000000")]
		[TestCase("800000000000000000000000")]
		[TestCase("ffffffffffffffffffffffff")]
		public void ParseObjectId(string text)
		{
			var bound = BsonBound.Parse($"{{ \"_id\" : ObjectId(\"{text}\") }}");

			var element = ((BsonDocument) bound).Elements.Single();

			Assert.That(element.Value.AsObjectId, Is.EqualTo(ObjectId.Parse(text)));
		}

		[Test]
		public void ParseInt()
		{
			var bound = BsonBound.Parse($"{{ \"_id\" : NumberInt(10) }}");

			var element = ((BsonDocument) bound).Elements.Single();

			Assert.That(element.Value.AsInt32, Is.EqualTo(10));
		}
	}
}
