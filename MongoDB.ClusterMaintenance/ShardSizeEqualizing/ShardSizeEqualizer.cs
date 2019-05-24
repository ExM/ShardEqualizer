using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.ClusterMaintenance.Models;
using MongoDB.ClusterMaintenance.MongoCommands;

namespace MongoDB.ClusterMaintenance.ShardSizeEqualizing
{
	public partial class ShardSizeEqualizer
	{
		private readonly ChunkCollection _chunks;
		private readonly DatasizeCache _datasize;
		private readonly IReadOnlyList<Zone> _zones;
		private readonly IReadOnlyList<Bound> _movingBounds;
		
		private readonly HashSet<BoundState> _boundStates = new HashSet<BoundState>();
		
		public ShardSizeEqualizer(IReadOnlyCollection<Shard> shards, IReadOnlyDictionary<ShardIdentity, CollStats> collStatsByShards,
			IReadOnlyList<TagRange> tagRanges, ChunkCollection chunks, Func<string, Task<long>> chunkSizeResolver)
		{
			_chunks = chunks;
			_datasize = new DatasizeCache(chunkSizeResolver);

			continuityCheck(tagRanges);
			
			_zones = tagRanges
				.Select(r => new { tagId = r.Tag, shardId = shards.Single(s => s.Tags.Contains(r.Tag)).Id})
				.Select(i => new Zone(i.shardId, i.tagId, collStatsByShards[i.shardId].Size))
				.ToList();

			var leftFixedBound = new Bound(this, tagRanges.First().Min);
			if (leftFixedBound.RightChunk == null)
				throw new Exception($"First chunk not found by first bound of tags");
			_zones.First().Left = leftFixedBound;
			
			var rightFixedBound = new Bound(this, tagRanges.Last().Max);
			if(rightFixedBound.LeftChunk == null)
				throw new Exception($"Last chunk not found by last bound of tags");
			_zones.Last().Right = rightFixedBound;

			_movingBounds = tagRanges.Skip(1).Select(item => new Bound(this, item.Min)).ToList();

			var zoneIndex = 0;
			foreach (var bound in _movingBounds)
			{
				_zones[zoneIndex].Right = bound;
				_zones[zoneIndex + 1].Left = bound;
				zoneIndex++;
			}

			checkCycle();
		}

		private bool checkCycle()
		{
			return !_boundStates.Add(new BoundState(_movingBounds.Select(_ => _.Value).ToArray()));
		}

		public IReadOnlyList<Bound> MovingBounds => _movingBounds;

		public IReadOnlyList<Zone> Zones => _zones;

		public long CurrentSizeDeviation
		{
			get
			{
				var minSize = _zones.Select(_ => _.BalanceSize).Min();
				var maxSize = _zones.Select(_ => _.BalanceSize).Max();
				return maxSize - minSize;
			}
		}

		public string RenderState()
		{
			var sb = new StringBuilder($"[{_movingBounds.First().LeftZone.Main}]");
			
			foreach (var bound in _movingBounds)
			{
				var shiftSize = bound.ShiftSize;
				var target = " | ";
				if (shiftSize > 0)
					target = " > ";
				else if (shiftSize < 0)
				{
					target = " < ";
					shiftSize *= -1;
				}

				sb.Append(target);
				sb.Append(shiftSize.ByteSize());
				sb.Append(target);
				sb.Append($"[{bound.RightZone.Main}]");
			}

			return sb.ToString();
		}
		
		public async Task<bool> Equalize()
		{
			Bound candidate = null;
			long maxDelta = 0;

			foreach (var bound in _movingBounds)
			{
				var candidateDelta = await bound.CalcMoveDelta();
				if (maxDelta < candidateDelta)
				{
					maxDelta = candidateDelta;
					candidate = bound;
				}
			}

			if (candidate == null)
				return false;
			
			await candidate.Move();
			
			var cycle =  checkCycle();
			if (cycle)
				return false;

			return true;
		}

		private static void continuityCheck(IReadOnlyCollection<TagRange> tagRanges)
		{
			var nextBound = tagRanges.First().Max;
			
			foreach (var range in tagRanges.Skip(1))
			{
				if (range.Min != nextBound)
					throw new ArgumentException($"found discontinuity from {range.Min.ToJson()} to {nextBound}");

				nextBound = range.Max;
			}
		}
	}
}