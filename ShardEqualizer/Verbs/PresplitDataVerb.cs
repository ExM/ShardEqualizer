using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using Ninject;
using ShardEqualizer.Operations;

namespace ShardEqualizer.Verbs
{
	[Verb("presplit", HelpText = "Split and distribute existing chunks among zones.")]
	public class PresplitDataVerb : BaseCommandFileVerb
	{
		[Option("renew", Required = false, Default = false, HelpText = "Recreate unchanged zones order.")]
		public bool Renew { get; set; }

		protected override async Task RunOperation(IKernel kernel, CancellationToken token)
		{
			kernel.Bind<IOperation>().To<PresplitDataOperation>()
				.WithConstructorArgument(Renew);

			await base.RunOperation(kernel, token);
		}
	}
}
