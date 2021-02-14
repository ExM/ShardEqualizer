using System;
using MongoDB.Driver;
using ShardEqualizer.Models;

namespace ShardEqualizer.ShardSizeEqualizing
{
	public class Bucket : IEquatable<Bucket>
	{
		private long _minSize;

		public Bucket(CollectionNamespace collection, ShardIdentity shard)
		{
			Shard = shard;
			Collection = collection;
			CurrentSize = 0;
			TargetSize = 0;
			Managed = false;
			_minSize = 0;
		}

		public ShardIdentity Shard { get; }
		public CollectionNamespace Collection { get; }
		public long CurrentSize { get; set; }
		public long TargetSize { get; set; }
		public bool Managed { get; set; }

		public long MinSize
		{
			get => _minSize;
			set => _minSize = value < 0 ? 0 : value;
		}

		public bool Equals(Bucket other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return Shard.Equals(other.Shard) && Equals(Collection, other.Collection);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != this.GetType()) return false;
			return Equals((Bucket) obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return (Shard.GetHashCode() * 397) ^ (Collection != null ? Collection.GetHashCode() : 0);
			}
		}

		public override string ToString()
		{
			return $"{Shard}:{Collection}";
		}
	}
}
