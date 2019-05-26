using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.ClusterMaintenance.Models;
using MongoDB.ClusterMaintenance.MongoCommands;
using NLog;

namespace MongoDB.ClusterMaintenance.ShardSizeEqualizing
{
	public partial class ShardSizeEqualizer
	{
		private static readonly Logger _log = LogManager.GetCurrentClassLogger();

		private readonly IReadOnlyList<Bound> _movingBounds;
		
		public ShardSizeEqualizer(
			IReadOnlyCollection<Shard> shards,
			IReadOnlyDictionary<ShardIdentity, CollStats> collStatsByShards,
			IReadOnlyList<TagRange> tagRanges,
			IDictionary<TagIdentity, long> sizeCorrection,
			ChunkCollection chunks)
		{
			continuityCheck(tagRanges);
			
			Zones = tagRanges
				.Select(r => new { tagId = r.Tag, shardId = shards.Single(s => s.Tags.Contains(r.Tag)).Id})
				.Select(i => new Zone(i.shardId, i.tagId, collStatsByShards[i.shardId].Size, sizeCorrection?[i.tagId] ?? 0))
				.ToList();

			var leftFixedBound = new Bound(chunks, tagRanges.First().Min);
			if (leftFixedBound.RightChunk == null)
				throw new Exception($"First chunk not found by first bound of tags");
			Zones.First().Left = leftFixedBound;
			
			var rightFixedBound = new Bound(chunks, tagRanges.Last().Max);
			if(rightFixedBound.LeftChunk == null)
				throw new Exception($"Last chunk not found by last bound of tags");
			Zones.Last().Right = rightFixedBound;

			_movingBounds = tagRanges.Skip(1).Select(item => new Bound(chunks, item.Min)).ToList();

			var zoneIndex = 0;
			foreach (var bound in _movingBounds)
			{
				Zones[zoneIndex].Right = bound;
				Zones[zoneIndex + 1].Left = bound;
				zoneIndex++;
			}
			
			var avgSize = Zones.Sum(_ => _.BalanceSize) / Zones.Count;
			long toRight = 0;
			foreach (var bound in _movingBounds)
			{
				toRight += avgSize - bound.LeftZone.BalanceSize;;
				bound.RequireShiftSize = toRight;
			}
		}

		public IReadOnlyList<Zone> Zones { get; }

		public long CurrentSizeDeviation
		{
			get
			{
				var minSize = Zones.Select(_ => _.BalanceSize).Min();
				var maxSize = Zones.Select(_ => _.BalanceSize).Max();
				return maxSize - minSize;
			}
		}
		
		public long MovedSize => _movingBounds.Sum(_ => Math.Abs(_.ShiftSize));

		public long RequireMoveSize => _movingBounds.Sum(_ => Math.Abs(_.RequireShiftSize));

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
				sb.Append($"{shiftSize.ByteSize()} ({bound.LeftChunk.Order})");
				sb.Append(target);
				sb.Append($"[{bound.RightZone.Main}]");
			}

			return sb.ToString();
		}
		
		public async Task<bool> Equalize()
		{
			foreach (var bound in _movingBounds)
				if (await bound.TryMove())
					return true;

			return false;
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