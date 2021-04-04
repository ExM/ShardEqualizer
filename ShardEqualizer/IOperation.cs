using System.Threading;
using System.Threading.Tasks;

namespace ShardEqualizer
{
	public interface IOperation
	{
		Task Run(CancellationToken token);
	}
}