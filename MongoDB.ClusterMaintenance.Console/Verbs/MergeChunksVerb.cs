using CommandLine;
using MongoDB.ClusterMaintenance.Operations;
using Ninject;

namespace MongoDB.ClusterMaintenance.Verbs
{
	[Verb("merge", HelpText = "Merge empty or small chunks")]
	public class MergeChunksVerb: BaseOptions
	{
		public override void BindOperation(IKernel kernel)
		{
			kernel.Bind<IOperation>().To<MergeChunksOperation>();
		}
	}
}
