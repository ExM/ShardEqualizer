using System.Collections.Generic;
using CommandLine;
using MongoDB.ClusterMaintenance.Operations;
using Ninject;

namespace MongoDB.ClusterMaintenance.Verbs
{
	[Verb("scan", HelpText = "Scan chunks")]
	public class ScanChunksVerb : BaseOptions
	{
		[Option("sizes", Separator = ',', Required = false, HelpText = "additional sizes of chunks")]
		public IList<string> Sizes { get; set; }

		public override void BindOperation(IKernel kernel)
		{
			kernel.Bind<IOperation>().To<ScanChunksOperation>()
				.WithConstructorArgument(Sizes);
		}
	}
}
