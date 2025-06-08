using System;
using System.Linq;
using System.Text;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using NUnit.Framework;
using ShardEqualizer.DAL.Models;

namespace ShardEqualizer
{
	[TestFixture]
	public class BsonSplitterTests
	{
		[SetUp]
		public void Setup()
		{
		}

		[Test]
		public void GuidBounds()
		{
			var guidMin = new BsonBinaryData(Guid.Empty, GuidRepresentation.Standard);
			var guidMax = new BsonBinaryData(Guid.Parse("FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF"), GuidRepresentation.Standard);
			
			var bounds = BsonSplitter.Split(guidMin, guidMax, 13);
			var docBounds = bounds.Select(_ => new BsonBound(new BsonDocument("x", _))).ToList();

			docBounds.Insert(0, new BsonBound(new BsonDocument("x",  guidMin)));
			docBounds.Add(new BsonBound(new BsonDocument("x",  guidMax)));

			foreach (var pair in docBounds.Take(docBounds.Count - 1).Zip(docBounds.Skip(1), (x, y) => new { x, y}))
			{
				Assert.That(pair.x < pair.y, Is.True);
			}

			foreach (var bound in bounds)
			{
				var hex = ByteArrayToString(((BsonBinaryData) bound).Bytes);

				var jsonSettings = new JsonWriterSettings() {};

				Console.WriteLine("{0} {1}", hex, bound.ToJson(jsonSettings));
			}
		}

		[Test]
		public void InvObjectIdBounds()
		{
			var invOidMin = (BsonValue) ObjectId.Parse("800000000000000000000000");
			var invOidMax = (BsonValue) ObjectId.Parse("ffffffffffffffffffffffff");

			var bounds = BsonSplitter.Split(invOidMin, invOidMax, 17);

			var docBounds = bounds.Select(_ => new BsonBound(new BsonDocument("x", _))).ToList();

			docBounds.Insert(0, new BsonBound(new BsonDocument("x",  invOidMin)));
			docBounds.Add(new BsonBound(new BsonDocument("x",  invOidMax)));

			foreach (var pair in docBounds.Take(docBounds.Count - 1).Zip(docBounds.Skip(1), (x, y) => new { x, y}))
			{
				Assert.That(pair.x < pair.y, Is.True);
			}


			foreach (var bound in bounds)
			{
				var hex = ByteArrayToString(((ObjectId) bound).ToByteArray());

				var jsonSettings = new JsonWriterSettings() {};

				Console.WriteLine("{0} {1}", hex, bound.ToJson(jsonSettings));
			}
		}

		public static string ByteArrayToString(byte[] ba)
		{
			StringBuilder hex = new StringBuilder(ba.Length * 2);
			foreach (byte b in ba)
				hex.AppendFormat("{0:x2}", b);
			return hex.ToString();
		}
	}
}
