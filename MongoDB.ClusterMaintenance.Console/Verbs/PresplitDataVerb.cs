using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using MongoDB.ClusterMaintenance.Operations;
using Ninject;

namespace MongoDB.ClusterMaintenance.Verbs
{
	[Verb("presplit", HelpText = "distribute data by zones with splitting existing chunks")]
	public class PresplitDataVerb : BaseCommandFileVerb
	{
		public override Task RunOperation(IKernel kernel, CancellationToken token)
		{
			kernel.Bind<IOperation>().To<PresplitDataOperation>();

			return base.RunOperation(kernel, token);
		}
	}
}
