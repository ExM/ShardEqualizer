using ShardEqualizer.DAL.Models;
using ShardEqualizer.ShardedClusterViews.Models;

namespace ShardEqualizer.ShardedClusterViews.ChunkCaching;

public class ChunksCache
{
	private readonly List<ChunkInfo> _chunks;

	public ChunksCache(List<ChunkInfo> chunks)
	{
		_chunks = chunks;
	}

	public ChunkInfo[] FromInterval(BsonBound? min, BsonBound? max)
	{
		var start = FindStartChunk(min);

		if (start >= _chunks.Count)
			return [];

		var end = FindEndChunk(max);

		if(end - start == 0)
			return [];

		return _chunks.Skip(start).Take(end - start + 1).ToArray();
	}

	private int FindStartChunk(BsonBound? key)
	{
		if (key == null)
			return 0;

		var pos = FindLeftChunkIndex(key.Value);
		if (pos == -1)
			return 0;

		return pos;
	}

	private int FindEndChunk(BsonBound? key)
	{
		if (key == null)
			return _chunks.Count - 1;

		var pos = FindLeftChunkIndex(key.Value);
		if (pos == -1)
			return 0;

		if(pos >= _chunks.Count)
			return _chunks.Count - 1;

		return pos;
	}

	private int FindLeftChunkIndex(BsonBound key)
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