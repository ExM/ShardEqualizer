using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace ShardEqualizer.LocalStoring
{
	public interface INsLocalStore<T>
	{
		Task<T> Get(CollectionNamespace ns, CancellationToken token);
	}
}