using System;
using System.Threading;
using System.Threading.Tasks;

namespace ShardEqualizer.LocalStoring
{
	public class BypassStore<T>: ILocalStore<T>
	{
		private readonly Func<CancellationToken, Task<T>> _uploadData;

		public BypassStore(Func<CancellationToken, Task<T>> uploadData)
		{
			_uploadData = uploadData;
		}

		public Task<T> Get(CancellationToken token)
		{
			return _uploadData(token);
		}
	}
}