using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using MongoDB.Bson;
using MongoDB.ClusterMaintenance.Models;

namespace MongoDB.ClusterMaintenance
{
	public static class BsonSplitter
	{
		public static IList<BsonBound> SplitFirstValue(BsonBound min, BsonBound max, int zonesCount)
		{
			return SplitFirstValue((BsonDocument) min, (BsonDocument) max, zonesCount).Select(_=> (BsonBound)_).ToList();
		}
		
		public static IList<BsonDocument> SplitFirstValue(BsonDocument min, BsonDocument max, int zonesCount)
		{
			if(!min.Elements.Select(_ => _.Name).SequenceEqual(max.Elements.Select(_ => _.Name), StringComparer.Ordinal))
				throw new InvalidOperationException($"within the boundaries of the chunks should be equivalent sets of elements");

			var firstElementName = min.Elements.Select(_ => _.Name).First();

			return Split(min[firstElementName], max[firstElementName], zonesCount).Select(_ =>
			{
				var bound = (BsonDocument) min.DeepClone();
				bound[firstElementName] = _;
				return bound;
			}).ToList();
		}
		
		public static IList<BsonValue> Split(BsonValue min, BsonValue max, int zonesCount)
		{
			if(min >= max)
				throw new InvalidOperationException($"min value {min} must be less max value {max}");

			if (IsUuidLegacy(min) && IsUuidLegacy(max))
			{
				return Split(min.AsByteArray, max.AsByteArray, zonesCount)
					.Select(_ => (BsonValue)new BsonBinaryData(_, BsonBinarySubType.UuidLegacy, GuidRepresentation.CSharpLegacy)).ToList();
			}
			
			if (IsUuidStandard(min) && IsUuidStandard(max))
			{
				return Split(min.AsByteArray, max.AsByteArray, zonesCount)
					.Select(_ => (BsonValue)new BsonBinaryData(_, BsonBinarySubType.UuidStandard, GuidRepresentation.Standard)).ToList();
			}
			
			if (min.IsObjectId && max.IsObjectId)
			{
				return Split(min.AsObjectId.ToByteArray(), max.AsObjectId.ToByteArray(), zonesCount)
					.Select(_ => (BsonValue)new BsonObjectId(new ObjectId(_))).ToList();
			}
			
			throw new NotImplementedException($"unexpected BsonValue type {min.BsonType} and {max.BsonType}");
		}

		public static IEnumerable<byte[]> Split(byte[] minBytes, byte[] maxBytes, int zonesCount)
		{
			if(minBytes.Length != maxBytes.Length)
				throw new InvalidOperationException("array length not equals");

			var length = minBytes.Length;

			var minHex = "00" + ByteArrayToString(minBytes);
			var maxHex = "00" + ByteArrayToString(maxBytes);
			
			var min = BigInteger.Parse(minHex, NumberStyles.HexNumber);
			var max = BigInteger.Parse(maxHex, NumberStyles.HexNumber);
			
			if(zonesCount <= 1)
				throw new InvalidOperationException("zones count must be great than 1");
			
			if(min >= max)
				throw new InvalidOperationException($"min value {min} must be less max value {max}");

			var delta = (max - min);

			for (int i = 0; i < zonesCount - 1; i++)
			{
				var bound = min + delta * (i + 1) / zonesCount;
				var boundHex = bound.ToString("X").PadLeft(length * 2, '0');
				if (boundHex.Length > length * 2)
					boundHex = boundHex.Substring(boundHex.Length - length * 2, length * 2);

				yield return HexStringToByteArray(boundHex);
			}
		}
		
		public static string ByteArrayToString(byte[] ba)
		{
			StringBuilder hex = new StringBuilder(ba.Length * 2);
			foreach (byte b in ba)
				hex.AppendFormat("{0:x2}", b);
			return hex.ToString();
		}

		public static byte[] HexStringToByteArray(string hexString)
		{
			if (hexString.Length % 2 != 0)
			{
				throw new ArgumentException(String.Format(CultureInfo.InvariantCulture, "The binary key cannot have an odd number of digits: {0}", hexString));
			}

			byte[] data = new byte[hexString.Length / 2];
			for (int index = 0; index < data.Length; index++)
			{
				string byteValue = hexString.Substring(index * 2, 2);
				data[index] = byte.Parse(byteValue, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
			}

			return data; 
		}

		private static bool IsUuidLegacy(BsonValue value)
		{
			if (value.BsonType != BsonType.Binary)
				return false;
			var binData = (BsonBinaryData) value;
			return binData.SubType == BsonBinarySubType.UuidLegacy && binData.Bytes.Length == 16;
		}
		
		private static bool IsUuidStandard(BsonValue value)
		{
			if (value.BsonType != BsonType.Binary)
				return false;
			var binData = (BsonBinaryData) value;
			return binData.SubType == BsonBinarySubType.UuidStandard && binData.Bytes.Length == 16;
		}
	}
}