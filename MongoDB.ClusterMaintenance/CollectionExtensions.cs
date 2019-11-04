using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
		
		public static async Task<IReadOnlyList<R>> ParallelsAsync<S, R>(this IList<S> sourceList, Func<S, CancellationToken, Task<R>> actionTask, int maxParallelizm, CancellationToken token)
		{
			var collTasks = new List<Task<R>>(sourceList.Count);
			var throttler = new SemaphoreSlim(maxParallelizm);

			async Task<R> runAction(S source)
			{
				try
				{
					return await actionTask(source, token);
				}
				finally
				{
					throttler.Release();
				}
			}

			foreach (var source in sourceList)
			{
				await throttler.WaitAsync(token);
				collTasks.Add(runAction(source));
			}

			return await Task.WhenAll(collTasks);
		}
		
		public static async Task ParallelsAsync<S>(this IList<S> sourceList, Func<S, CancellationToken, Task> actionTask, int maxParallelizm, CancellationToken token)
		{
			var collTasks = new List<Task>(sourceList.Count);
			var throttler = new SemaphoreSlim(maxParallelizm);

			async Task runAction(S source)
			{
				try
				{
					await actionTask(source, token);
				}
				finally
				{
					throttler.Release();
				}
			}

			foreach (var source in sourceList)
			{
				await throttler.WaitAsync(token);
				collTasks.Add(runAction(source));
			}

			await Task.WhenAll(collTasks);
		}
	}
}