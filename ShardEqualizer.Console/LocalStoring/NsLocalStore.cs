using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace ShardEqualizer.LocalStoring
{
	public class NsLocalStore<T>: BaseLocalStore<T>, INsLocalStore<T>
	{
		private readonly string _basePath;
		private readonly Func<CollectionNamespace, CancellationToken, Task<T>> _uploadData;

		public NsLocalStore(string basePath, Func<CollectionNamespace, CancellationToken, Task<T>> uploadData,
			bool read, bool write): base(read, write)
		{
			_basePath = basePath;

			if(!Directory.Exists(_basePath))
				Directory.CreateDirectory(_basePath);

			_uploadData = uploadData;
		}

		public Task<T> Get(CollectionNamespace ns, CancellationToken token)
		{
			var fileName = Path.Combine(_basePath, $"{ns}.bson");
			return Get(fileName, t => _uploadData(ns, t), token);
		}
	}
}