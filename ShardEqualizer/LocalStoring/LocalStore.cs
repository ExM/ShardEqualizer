using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ShardEqualizer.LocalStoring
{
	public class LocalStore<T>: BaseLocalStore<T>, ILocalStore<T>
	{
		private readonly Func<CancellationToken, Task<T>> _uploadData;
		private readonly string _fileName;

		public LocalStore(string basePath, string name, Func<CancellationToken, Task<T>> uploadData,
			bool read, bool write): base(read, write)
		{
			_fileName = Path.Combine(basePath, $"{name}.bson");
			_uploadData = uploadData;
		}

		public Task<T> Get(CancellationToken token)
		{
			return Get(_fileName, _uploadData, token);
		}
	}
}