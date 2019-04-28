using System;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.ClusterMaintenance.Models;

namespace MongoDB.ClusterMaintenance.ShardSizeEqualizing
{
	public partial class ShardSizeEqualizer
	{
		public class Bound
		{
			private readonly ShardSizeEqualizer _shardSizeEqualizer;
			public Zone LeftZone;
			public Chunk LeftChunk;
			public Chunk LeftNextChunk;
			public Zone RightZone;
			public Chunk RightChunk;
			public Chunk RightNextChunk;
			public BsonBound Value { get; set; }
			public long ShiftSize => _shiftSize;
			private long _shiftSize = 0;

			public Bound(ShardSizeEqualizer shardSizeEqualizer, BsonBound value)
			{
				_shardSizeEqualizer = shardSizeEqualizer;
				Value = value;
				LeftChunk = _shardSizeEqualizer._chunks.FindLeft(value);
				RightChunk = _shardSizeEqualizer._chunks.FindRight(value);
			}

			public async Task<long> CalcMoveDelta()
			{
				if (LeftZone.CurrentSize == RightZone.CurrentSize)
					return 0;

				if (LeftZone.CurrentSize < RightZone.CurrentSize)
					return await findRightNextChunk();
				else
					return await findLeftNextChunk();
			}

			private async Task<long> findRightNextChunk()
			{
				var stopChunkId = RightZone.Right.LeftChunk.Id;
				RightNextChunk = null;

				var candidate = RightChunk;

				while (true)
				{
					if (candidate.Id == stopChunkId)
						return 0;

					if (!candidate.Jumbo)
					{
						var chunkSize = await _shardSizeEqualizer._datasize.Get(candidate.Id);
						if (chunkSize != 0)
						{
							RightNextChunk = candidate;
							return RightZone.CurrentSize - LeftZone.CurrentSize;
						}
					}

					candidate = _shardSizeEqualizer._chunks.FindRight(candidate);
					if (candidate == null)
						return 0;
				}
			}

			private async Task<long> findLeftNextChunk()
			{
				var stopChunkId = LeftZone.Left.RightChunk.Id;
				LeftNextChunk = null;

				var candidate = LeftChunk;

				while (true)
				{
					if (candidate.Id == stopChunkId)
						return 0;

					if (!candidate.Jumbo)
					{
						var chunkSize = await _shardSizeEqualizer._datasize.Get(candidate.Id);
						if (chunkSize != 0)
						{
							LeftNextChunk = candidate;
							return LeftZone.CurrentSize - RightZone.CurrentSize;
						}
					}

					candidate = _shardSizeEqualizer._chunks.FindLeft(candidate);
					if (candidate == null)
						return 0;
				}
			}

			public async Task Move()
			{
				if (LeftZone.CurrentSize == RightZone.CurrentSize)
					throw new Exception();

				if (LeftZone.CurrentSize < RightZone.CurrentSize)
					await moveToRight();
				else
					await moveToLeft();
			}

			private async Task moveToRight()
			{
				if (RightNextChunk == null)
					throw new Exception();

				var chunkSize = await _shardSizeEqualizer._datasize.Get(RightNextChunk.Id);

				Interlocked.Add(ref _shiftSize, chunkSize);
				RightZone.CurrentSize -= chunkSize;
				LeftZone.CurrentSize += chunkSize;
				LeftChunk = RightNextChunk;
				Value = RightNextChunk.Max;

				RightNextChunk = null;
				LeftNextChunk = null;

				RightChunk = _shardSizeEqualizer._chunks.FindRight(LeftChunk);
			}

			private async Task moveToLeft()
			{
				if (LeftNextChunk == null)
					throw new Exception();

				var chunkSize = await _shardSizeEqualizer._datasize.Get(LeftNextChunk.Id);

				Interlocked.Add(ref _shiftSize, -chunkSize);
				RightZone.CurrentSize += chunkSize;
				LeftZone.CurrentSize -= chunkSize;
				RightChunk = LeftNextChunk;
				Value = LeftNextChunk.Min;

				RightNextChunk = null;
				LeftNextChunk = null;

				LeftChunk = _shardSizeEqualizer._chunks.FindLeft(RightChunk);
			}
		}
	}
}