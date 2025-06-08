using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using Ninject;
using ShardEqualizer.Operations;

namespace ShardEqualizer.Verbs
{
	[Verb("clear-jumbo", HelpText = "Manually clear the jumbo flag of all chunks.")]
	public class ClearJumboChunksVerb: BaseCommandFileVerb
	{
		protected override async Task RunOperation(IKernel kernel, CancellationToken token)
		{
			kernel.Bind<IOperation>().To<ClearJumboChunksOperation>();

			await base.RunOperation(kernel, token);
		}
	}
}
