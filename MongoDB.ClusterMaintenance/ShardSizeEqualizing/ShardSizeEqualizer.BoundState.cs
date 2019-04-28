using System;
using System.Linq;
using MongoDB.Bson;
using MongoDB.ClusterMaintenance.Models;

namespace MongoDB.ClusterMaintenance.ShardSizeEqualizing
{
	public partial class ShardSizeEqualizer
	{
		private class BoundState: IEquatable<BoundState>
		{
			private readonly BsonBound[] _bounds;

			public BoundState(BsonBound[] bounds)
			{
				_bounds = bounds;
			}

			public bool Equals(BoundState other)
			{
				if (other == null)
					return false;
				
				if (_bounds.Length != other._bounds.Length)
					return false;

				for (var i = 0; i < _bounds.Length; i++)
					if (_bounds[i] != other._bounds[i])
						return false;

				return true;
			}

			public override bool Equals(object obj)
			{
				return Equals(obj as BoundState);;
			}

			public override int GetHashCode()
			{
				unchecked
				{
					return _bounds.Aggregate(13, (current, bound) => current * 7 + bound.GetHashCode());
				}
			}
		}
	}
}