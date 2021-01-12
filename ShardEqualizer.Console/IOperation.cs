using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.ClusterMaintenance
{
	public interface IOperation
	{
		Task Run(CancellationToken token);
	}
}