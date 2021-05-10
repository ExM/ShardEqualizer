using System;
using System.Collections.Generic;
using System.Linq;
using ShardEqualizer.Models;

namespace ShardEqualizer
{
	public static class ShardTagCollator
	{
		public static IDictionary<TagIdentity, Shard> Collate(IEnumerable<Shard> shards, IEnumerable<TagIdentity> usedTags)
		{
			return usedTags.Distinct().ToDictionary(x => x, x => SelectOnlyOneShardByTag(shards, x));
		}

		private static Shard SelectOnlyOneShardByTag(IEnumerable<Shard> shards, TagIdentity tag)
		{
			using var e = shards.GetEnumerator();
			while (e.MoveNext())
			{
				var result = e.Current;
				if(result == null)
					continue;
				if (!result.Tags.Contains(tag)) continue;

				while (e.MoveNext())
				{
					if (e.Current.Tags.Contains(tag))
						throw new Exception($"shard '{result.Id}' and '{e.Current.Id}' both contains one tag zone '{tag}'");
				}

				return result;
			}

			throw new Exception($"no shard was found containing the tag zone '{tag}'");
		}
	}
}
