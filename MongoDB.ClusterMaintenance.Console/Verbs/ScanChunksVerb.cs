using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using MongoDB.ClusterMaintenance.Operations;
using Ninject;

namespace MongoDB.ClusterMaintenance.Verbs
{
	[Verb("scan", HelpText = "Scan chunks")]
	public class ScanChunksVerb : BaseVerbose
	{
		[Option("sizes", Separator = ',', Required = false, HelpText = "additional sizes of chunks")]
		public IList<string> Sizes { get; set; }

		public override async Task RunOperation(IKernel kernel, CancellationToken token)
		{
			kernel.Bind<IOperation>().To<ScanChunksOperation>()
				.WithConstructorArgument(Sizes);
			
			await kernel.Get<IOperation>().Run(token);
		}
	}
}
