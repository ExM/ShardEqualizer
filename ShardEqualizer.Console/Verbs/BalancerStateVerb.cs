using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using MongoDB.ClusterMaintenance.Operations;
using Ninject;

namespace MongoDB.ClusterMaintenance.Verbs
{
	[Verb("balanser", HelpText = "Show state of shards balancer and the end of the movement of shards")]
	public class BalancerStateVerb: BaseVerbose
	{
		public override async Task RunOperation(IKernel kernel, CancellationToken token)
		{
			kernel.Bind<IOperation>().To<BalancerStateOperation>();

			await kernel.Get<IOperation>().Run(token);

		}
	}
}