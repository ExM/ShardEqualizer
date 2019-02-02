using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.ClusterMaintenance.Models;

namespace MongoDB.ClusterMaintenance
{
	public class ChunkCollection
	{
		private readonly IDictionary<string, Chunk> _idMap;
		private readonly IDictionary<BsonDocument, Chunk> _maxMap = new Dictionary<BsonDocument, Chunk>();
		private readonly IDictionary<BsonDocument, Chunk> _minMap = new Dictionary<BsonDocument, Chunk>();
		
		public ChunkCollection(IReadOnlyList<Chunk> chunks)
		{
			var firstChunk = chunks.First();
			_minMap.Add(firstChunk.Min, firstChunk);
			_maxMap.Add(firstChunk.Max, firstChunk);
			var nextBound = firstChunk.Max;

			foreach (var chunk in chunks.Skip(1))
			{
				if (chunk.Min != nextBound)
					throw new ArgumentException($"found discontinuity from {chunk.Min.ToJson()} to {nextBound}");
				nextBound = chunk.Max;
				_minMap.Add(chunk.Min, chunk);
				_maxMap.Add(chunk.Max, chunk);
			}

			_idMap = chunks.ToDictionary(_ => _.Id);
		}

		public Chunk ById(string id)
		{
			return _idMap[id];
		}

		public Chunk FindRight(BsonDocument value)
		{
			return _minMap.TryGetValue(value, out var result) ? result : null;
		}
		
		public Chunk FindLeft(BsonDocument value)
		{
			return _maxMap.TryGetValue(value, out var result) ? result : null;
		}
		
		public Chunk FindRight(Chunk chunk)
		{
			return FindRight(chunk.Max);
		}
		
		public Chunk FindLeft(Chunk chunk)
		{
			return FindLeft(chunk.Min);
		}
	}
}