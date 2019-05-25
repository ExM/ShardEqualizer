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
			private readonly int[] _boundOrders;

			public BoundState(int[] boundOrders)
			{
				_boundOrders = boundOrders;
			}

			public bool Equals(BoundState other)
			{
				if (other == null)
					return false;
				
				if (_boundOrders.Length != other._boundOrders.Length)
					return false;

				for (var i = 0; i < _boundOrders.Length; i++)
					if (_boundOrders[i] != other._boundOrders[i])
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
					return _boundOrders.Aggregate(13, (current, bound) => current * 7 + bound.GetHashCode());
				}
			}
		}
	}
}