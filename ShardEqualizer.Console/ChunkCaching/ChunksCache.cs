using System.Collections.Generic;
using System.Linq;
using ShardEqualizer.Models;
using ShardEqualizer.ShortModels;

namespace ShardEqualizer.ChunkCaching
{
	public class ChunksCache
	{
		private readonly List<ChunkInfo> _chunks;

		public ChunksCache(List<ChunkInfo> chunks)
		{
			_chunks = chunks;
		}

		public ChunkInfo[] FromInterval(BsonBound? min, BsonBound? max)
		{
			var start = findStartChunk(min);

			if (start >= _chunks.Count)
				return new ChunkInfo[0];

			var end = findEndChunk(max);

			if(end - start == 0)
				return new ChunkInfo[0];

			return _chunks.Skip(start).Take(end - start + 1).ToArray();
		}

		private int findStartChunk(BsonBound? key)
		{
			if (key == null)
				return 0;

			var pos = findLeftChunkIndex(key.Value);
			if (pos == -1)
				return 0;

			return pos;
		}

		private int findEndChunk(BsonBound? key)
		{
			if (key == null)
				return _chunks.Count - 1;

			var pos = findLeftChunkIndex(key.Value);
			if (pos == -1)
				return 0;

			if(pos >= _chunks.Count)
				return _chunks.Count - 1;

			return pos;
		}

		private int findLeftChunkIndex(BsonBound key)
		{
			if(_chunks.Count == 0)
				return -1;

			var left = 0;
			if(_chunks[left].Min > key) return -1;
			var right = _chunks.Count - 1;
			if(_chunks[right].Min <= key) return right;

			while((right-left)>1)
			{
				var middle = (left+right)/2;
				if(_chunks[middle].Min > key)
					right = middle;
				else
					left = middle;
			}
			return left;
		}
	}
}
