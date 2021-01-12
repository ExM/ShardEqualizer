using System.Threading;
using System.Threading.Tasks;

namespace ShardEqualizer.WorkFlow
{
	public interface IWork
	{
		Task Apply(CancellationToken token);
	}
}