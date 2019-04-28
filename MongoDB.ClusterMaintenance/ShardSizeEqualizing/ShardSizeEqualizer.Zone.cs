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
			
			public long InitialSize;
			public long CurrentSize;
		
			public ShardIdentity Main { get; set; }
			public TagIdentity Tag { get; set; }

			public BsonBound Min => _left.Value;
			public BsonBound Max => _right.Value;
		}
	}
}