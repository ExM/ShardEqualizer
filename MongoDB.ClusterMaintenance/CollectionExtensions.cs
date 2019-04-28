using System;
using System.Collections.Generic;
using System.Linq;

namespace MongoDB.ClusterMaintenance
{
	public static class CollectionExtensions
	{
		public static IEnumerable<List<T>> Split<T>(this List<T> items, int partCount)
		{
			var partSize = items.Count / partCount;
			var extra = items.Count % partCount;
			var offset = 0;
			for (var i = 0; i < partCount; i++)
			{
				var currentPartSize = partSize;
				if (extra > 0)
				{
					currentPartSize++;
					extra--;
				}
				
				yield return items.Skip(offset).Take(currentPartSize).ToList();
				offset += currentPartSize;
			}
		}
	}
}