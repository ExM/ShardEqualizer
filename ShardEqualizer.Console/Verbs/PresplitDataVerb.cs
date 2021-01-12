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
		[Option("renew", Required = false, Default = false, HelpText = "recreate unchanged zones")]
		public bool Renew { get; set; }
		
		public override Task RunOperation(IKernel kernel, CancellationToken token)
		{
			kernel.Bind<IOperation>().To<PresplitDataOperation>()
				.WithConstructorArgument(Renew);

			return base.RunOperation(kernel, token);
		}
	}
}
