using System;
using System.Collections.Generic;
using System.Linq;

namespace MongoDB.ClusterMaintenance
{
	public static class CollectionExtensions
	{
		public static IEnumerable<IList<T>> Split<T>(this ICollection<T> items, 
			int numberOfChunks)
		{
			if (numberOfChunks <= 0 || numberOfChunks > items.Count)
				throw new ArgumentOutOfRangeException(nameof(numberOfChunks));

			var sizePerPacket = items.Count / numberOfChunks;
			var extra = items.Count % numberOfChunks;

			for (var i = 0; i < numberOfChunks - extra; i++)
				yield return items.Skip(i * sizePerPacket).Take(sizePerPacket).ToList();

			var alreadyReturnedCount = (numberOfChunks - extra) * sizePerPacket;
			var toReturnCount = extra == 0 ? 0 : (items.Count - numberOfChunks) / extra + 1;
			for (var i = 0; i < extra; i++)
				yield return items.Skip(alreadyReturnedCount + i * toReturnCount).Take(toReturnCount).ToList();
		}
	}
}