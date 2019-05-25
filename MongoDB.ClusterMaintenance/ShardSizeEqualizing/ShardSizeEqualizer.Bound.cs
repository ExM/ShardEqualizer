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
			private readonly ChunkCollection _chunks;
			public Zone LeftZone;
			public ChunkCollection.Entry LeftChunk;
			public ChunkCollection.Entry LeftNextChunk;
			public Zone RightZone;
			public ChunkCollection.Entry RightChunk;
			public ChunkCollection.Entry RightNextChunk;
			public BsonBound Value { get; set; }
			public long ShiftSize => _shiftSize;
			private long _shiftSize = 0;

			public Bound(ChunkCollection chunks, BsonBound value)
			{
				Value = value;
				_chunks = chunks;
				LeftChunk = _chunks.FindLeft(value);
				RightChunk =_chunks.FindRight(value);
			}

			public async Task<long> CalcMoveDelta()
			{
				if (LeftZone.BalanceSize == RightZone.BalanceSize)
					return 0;

				if (LeftZone.BalanceSize < RightZone.BalanceSize)
					return await findRightNextChunk();
				else
					return await findLeftNextChunk();
			}

			private async Task<long> findRightNextChunk()
			{
				var stopChunkId = RightZone.Right.LeftChunk.Chunk.Id;
				RightNextChunk = null;

				var candidate = RightChunk;

				while (true)
				{
					if (candidate.Chunk.Id == stopChunkId)
						return 0;

					if (!candidate.Chunk.Jumbo)
					{
						var chunkSize = await candidate.Size;
						if (chunkSize != 0)
						{
							RightNextChunk = candidate;
							return RightZone.BalanceSize - LeftZone.BalanceSize;
						}
					}

					candidate = _chunks.FindRight(candidate);
					if (candidate == null)
						return 0;
				}
			}

			private async Task<long> findLeftNextChunk()
			{
				var stopChunkId = LeftZone.Left.RightChunk.Chunk.Id;
				LeftNextChunk = null;

				var candidate = LeftChunk;

				while (true)
				{
					if (candidate.Chunk.Id == stopChunkId)
						return 0;

					if (!candidate.Chunk.Jumbo)
					{
						var chunkSize = await candidate.Size;
						if (chunkSize != 0)
						{
							LeftNextChunk = candidate;
							return LeftZone.BalanceSize - RightZone.BalanceSize;
						}
					}

					candidate = _chunks.FindLeft(candidate);
					if (candidate == null)
						return 0;
				}
			}

			public async Task Move()
			{
				if (LeftZone.BalanceSize == RightZone.BalanceSize)
					throw new Exception();

				if (LeftZone.BalanceSize < RightZone.BalanceSize)
					await moveToRight();
				else
					await moveToLeft();
			}

			private async Task moveToRight()
			{
				if (RightNextChunk == null)
					throw new Exception();

				var chunkSize = await RightNextChunk.Size;

				Interlocked.Add(ref _shiftSize, chunkSize);
				RightZone.SizeDown(chunkSize);
				LeftZone.SizeUp(chunkSize);
				LeftChunk = RightNextChunk;
				Value = RightNextChunk.Chunk.Max;

				RightNextChunk = null;
				LeftNextChunk = null;

				RightChunk = _chunks.FindRight(LeftChunk);
			}

			private async Task moveToLeft()
			{
				if (LeftNextChunk == null)
					throw new Exception();

				var chunkSize = await LeftNextChunk.Size;

				Interlocked.Add(ref _shiftSize, -chunkSize);
				RightZone.SizeUp(chunkSize);
				LeftZone.SizeDown(chunkSize);
				RightChunk = LeftNextChunk;
				Value = LeftNextChunk.Chunk.Min;

				RightNextChunk = null;
				LeftNextChunk = null;

				LeftChunk = _chunks.FindLeft(RightChunk);
			}
		}
	}
}