using System;
using System.Threading;
using System.Threading.Tasks;
using ShardEqualizer.DAL.Models;

namespace ShardEqualizer.ShardSizeEqualizing
{
	public partial class ShardSizeEqualizer
	{
		public class Bound
		{
			private readonly ShardSizeEqualizer _shardSizeEqualizer;
			private readonly ChunkCollection _chunks;
			public Zone LeftZone { get; set; }
			public ChunkCollection.Entry LeftChunk { get; private set; }
			public Zone RightZone { get; set; }
			public ChunkCollection.Entry RightChunk { get; private set; }
			public BsonBound Value { get; private set; }
			public long ShiftSize => _shiftSize;
			private long _shiftSize = 0;

			public long RequireShiftSize = 0;

			public long ElapsedShiftSize => Math.Abs(_shiftSize - RequireShiftSize);

			private ChunkCollection.Entry _nextChunk;
			
			public ChunkCollection.Entry UnMovedChunk { get; private set; }

			internal Bound(ShardSizeEqualizer shardSizeEqualizer, ChunkCollection chunks, BsonBound value)
			{
				Value = value;
				_shardSizeEqualizer = shardSizeEqualizer;
				_chunks = chunks;
				LeftChunk = _chunks.FindLeft(value);
				RightChunk =_chunks.FindRight(value);
			}

			public async Task<MoveResult> TryMove()
			{
				if (RequireShiftSize == 0)
					return MoveResult.Unsuccessful;

				long movedChunkSize;

				if (RequireShiftSize < 0)
				{ // to left
					if (_shiftSize <= RequireShiftSize)
					{
						UnMovedChunk = null;
						return MoveResult.Unsuccessful;
					}

					if (_nextChunk == null)
					{
						var (nextChunk, isLast) = await findLeftNextChunk();
						if (isLast)
						{
							UnMovedChunk = nextChunk;
							return MoveResult.Unsuccessful;
						}
						else
						{
							_nextChunk = nextChunk;
						}
					}

					if (_nextChunk == null)
					{
						UnMovedChunk = null;
						return MoveResult.Unsuccessful;
					}

					var nextChunkSize = await _nextChunk.Size;

					if ((_shiftSize - nextChunkSize / 2) < RequireShiftSize)
					{
						UnMovedChunk = _nextChunk;
						return MoveResult.Unsuccessful;
					}

					movedChunkSize = nextChunkSize;
					_shardSizeEqualizer.onChunkMoving(LeftZone, RightZone, this, nextChunkSize);

					Interlocked.Add(ref _shiftSize, -nextChunkSize);
					LeftZone.SizeDown(nextChunkSize);
					RightZone.SizeUp(nextChunkSize);
					RightChunk = _nextChunk;
					Value = _nextChunk.Chunk.Min;
					LeftChunk = _chunks.FindLeft(_nextChunk);
				}
				else // RequireShiftSize > 0
				{ // to right
					if (RequireShiftSize <= _shiftSize)
					{
						UnMovedChunk = null;
						return MoveResult.Unsuccessful;
					}

					if (_nextChunk == null)
					{
						var (nextChunk, isLast) = await findRightNextChunk();
						if (isLast)
						{
							UnMovedChunk = nextChunk;
							return MoveResult.Unsuccessful;
						}
						else
						{
							_nextChunk = nextChunk;
						}
					}

					if (_nextChunk == null)
					{
						UnMovedChunk = null;
						return MoveResult.Unsuccessful;
					}

					var nextChunkSize = await _nextChunk.Size;

					if ((_shiftSize + nextChunkSize / 2) > RequireShiftSize)
					{
						UnMovedChunk = _nextChunk;
						return MoveResult.Unsuccessful;
					}

					movedChunkSize = nextChunkSize;
					_shardSizeEqualizer.onChunkMoving(RightZone, LeftZone, this, nextChunkSize);

					Interlocked.Add(ref _shiftSize, nextChunkSize);
					LeftZone.SizeUp(nextChunkSize);
					RightZone.SizeDown(nextChunkSize);
					LeftChunk = _nextChunk;
					Value = _nextChunk.Chunk.Max;
					RightChunk = _chunks.FindRight(_nextChunk);
				}

				_nextChunk = null;
				UnMovedChunk = null;
				return new MoveResult(movedChunkSize);
			}

			private async Task<(ChunkCollection.Entry nextChunk, bool isLast)> findLeftNextChunk()
			{
				var stopEntry = LeftZone.Left.RightChunk;
				var candidate = LeftChunk;

				while (true)
				{
					if (candidate == stopEntry)
						return (candidate, true);

					if (!candidate.Chunk.Jumbo && await candidate.Size > 0)
						return (candidate, false);

					candidate = _chunks.FindLeft(candidate);
					if (candidate == null)
						return (null, false);
				}
			}

			private async Task<(ChunkCollection.Entry nextChunk, bool isLast)> findRightNextChunk()
			{
				var stopEntry =  RightZone.Right.LeftChunk;
				var candidate = RightChunk;

				while (true)
				{
					if (candidate == stopEntry)
						return (candidate, true);

					if (!candidate.Chunk.Jumbo && await candidate.Size > 0)
						return (candidate, false);

					candidate = _chunks.FindRight(candidate);
					if (candidate == null)
						return (null, false);
				}
			}
		}
	}
}
