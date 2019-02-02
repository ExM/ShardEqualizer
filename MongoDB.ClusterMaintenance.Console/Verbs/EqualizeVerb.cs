using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using MongoDB.ClusterMaintenance.Operations;
using Ninject;

namespace MongoDB.ClusterMaintenance.Verbs
{
	[Verb("equalize", HelpText = "alignment size shards by moving bound of zones")]
	public class EqualizeVerb: BaseCommandFileVerb
	{
		public override Task RunOperation(IKernel kernel, CancellationToken token)
		{
			kernel.Bind<IOperation>().To<EqualizeOperation>();
			
			return base.RunOperation(kernel, token);
		}
	}
}