using System.Collections.Generic;
using System.Linq;
using ShardEqualizer.DAL.Models;

namespace ShardEqualizer
{
	public static class CollectionExtensions
	{
		public static IReadOnlyList<TagRange> InRange(this IReadOnlyList<TagRange> tagRanges, BsonBound? intL, BsonBound? intR)
		{
			if(intL.HasValue && intR.HasValue)
				tagRanges = tagRanges.Where(r => IsIntersects(intL.Value, intR.Value, r.Min, r.Max)).ToList();

			return tagRanges;
		}

		private static bool IsIntersects(BsonBound intL, BsonBound intR, BsonBound chL, BsonBound chR)
		{
			return chL <= intR  && intL < chR;
		}
	}
}
