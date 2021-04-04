using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;
using ShardEqualizer.Config;

namespace ShardEqualizer.LocalStoring
{
	public class LocalStoreProvider
	{
		private readonly string _basePath;
		private readonly bool _read;
		private readonly bool _write;

		public LocalStoreProvider(LocalStoreConfig config)
		{
			_basePath = Path.GetFullPath(Path.Combine(".", "localStore"));

			_read = config.Read == true;
			_write = config.Write == true;

			if (Directory.Exists(_basePath))
			{
				if (config.Clean == true)
				{
					Directory.Delete(_basePath, true);
					if(_write)
						Directory.CreateDirectory(_basePath);
				}
			}
			else
				if(_write)
					Directory.CreateDirectory(_basePath);
		}

		public ILocalStore<T> Get<T>(string name, Func<CancellationToken, Task<T>> uploadData)
		{
			if (!_read && !_write)
				return new BypassStore<T>(uploadData);
			return new LocalStore<T>(_basePath, name, uploadData, _read, _write);
		}

		public INsLocalStore<T> Get<T>(string path, Func<CollectionNamespace, CancellationToken, Task<T>> uploadData)
		{
			if (!_read && !_write)
				return new NsBypassStore<T>(uploadData);
			return new NsLocalStore<T>(Path.Combine(_basePath, path), uploadData, _read, _write);
		}
	}
}
