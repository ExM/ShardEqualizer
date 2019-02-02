using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace MongoDB.ClusterMaintenance.ShardSizeEqualizing
{
	public partial class ShardSizeEqualizer
	{
		private class DatasizeCache
		{
			private readonly Func<string, Task<long>> _chunkSizeResolver;

			private readonly ConcurrentDictionary<string, Task<long>> _cache =
				new ConcurrentDictionary<string, Task<long>>();

			public DatasizeCache(Func<string, Task<long>> chunkSizeResolver)
			{
				_chunkSizeResolver = chunkSizeResolver;
			}

			public Task<long> Get(string chunkId)
			{
				return _cache.GetOrAdd(chunkId, _chunkSizeResolver);
			}
		}
	}
}