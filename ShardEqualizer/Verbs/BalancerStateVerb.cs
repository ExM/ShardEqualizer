using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using Ninject;
using ShardEqualizer.Operations;

namespace ShardEqualizer.Verbs
{
	[Verb("balancer", HelpText = "Show shard balancer state and current shard distribution.")]
	public class BalancerStateVerb: BaseVerbose
	{
		protected override async Task RunOperation(IKernel kernel, CancellationToken token)
		{
			kernel.Bind<IOperation>().To<BalancerStateOperation>();

			await base.RunOperation(kernel, token);
		}
	}
}
