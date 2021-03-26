using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace ShardEqualizer.LocalStoring
{
	public abstract class BaseLocalStore<T>
	{
		private readonly bool _read;
		private readonly bool _write;

		protected BaseLocalStore(bool read, bool write)
		{
			_read = read;
			_write = write;
		}

		protected async Task<T> Get(string fileName, Func<CancellationToken, Task<T>> uploadData, CancellationToken token)
		{
			if (_read && File.Exists(fileName))
			{
				await using var stream = File.OpenRead(fileName);
				return BsonSerializer.Deserialize<T>(stream);
			}

			var data = await uploadData(token);

			if (_write)
			{
				await using var stream = File.Open(fileName, FileMode.Create);
				using var bsonWriter = new BsonBinaryWriter(stream);
				BsonSerializer.Serialize(bsonWriter, data);
				bsonWriter.Flush();
				await stream.FlushAsync(token);
			}

			return data;
		}
	}
}