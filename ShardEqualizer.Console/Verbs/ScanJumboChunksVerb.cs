using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using Ninject;
using ShardEqualizer.Operations;

namespace ShardEqualizer.Verbs
{
	[Verb("scanJumbo", HelpText = "Scan jumbo chunks")]
	public class ScanJumboChunksVerb : BaseVerbose
	{
		public override async Task RunOperation(IKernel kernel, CancellationToken token)
		{
			kernel.Bind<IOperation>().To<ScanJumboChunksOperation>();
			
			await kernel.Get<IOperation>().Run(token);
		}
	}
}
