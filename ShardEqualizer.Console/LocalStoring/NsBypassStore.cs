using System;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace ShardEqualizer.LocalStoring
{
	public class NsBypassStore<T>: INsLocalStore<T>
	{
		private readonly Func<CollectionNamespace, CancellationToken, Task<T>> _uploadData;

		public NsBypassStore(Func<CollectionNamespace, CancellationToken, Task<T>> uploadData)
		{
			_uploadData = uploadData;
		}

		public Task<T> Get(CollectionNamespace ns, CancellationToken token)
		{
			return _uploadData(ns, token);
		}
	}
}