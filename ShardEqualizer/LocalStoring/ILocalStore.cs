using System.Threading;
using System.Threading.Tasks;

namespace ShardEqualizer.LocalStoring
{
	public interface ILocalStore<T>
	{
		Task<T> Get(CancellationToken token);
	}
}