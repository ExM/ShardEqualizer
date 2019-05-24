using System;
using System.Linq;
using MongoDB.Bson;
using MongoDB.ClusterMaintenance.Models;

namespace MongoDB.ClusterMaintenance.ShardSizeEqualizing
{
	public partial class ShardSizeEqualizer
	{
		public class Zone
		{
			private Bound _left;
			public Bound Left
			{
				get => _left;
				set
				{
					_left = value;
					_left.RightZone = this;
				}
			}
			
			private Bound _right;
			public Bound Right
			{
				get => _right;
				set
				{
					_right = value;
					_right.LeftZone = this;
				}
			}
			
			public long InitialSize  { get; }
			public long UnShardCorrection  { get; private set; }

			public Zone(ShardIdentity main, TagIdentity tag, long size)
			{
				Main = main;
				Tag = tag;
				InitialSize = size;
				CurrentSize = size;
			}

			public ShardIdentity Main { get; }
			public TagIdentity Tag { get; }

			public BsonBound Min => _left.Value;
			public BsonBound Max => _right.Value;

			public long CurrentSize { get; private set; }

			public long BalanceSize => CurrentSize + UnShardCorrection;

			public void SizeUp(long v)
			{
				CurrentSize += v;
			}
			
			public void SizeDown(long v)
			{
				CurrentSize -= v;
			}

			public void Correction(long v)
			{
				UnShardCorrection = v;
			}
		}
	}
}