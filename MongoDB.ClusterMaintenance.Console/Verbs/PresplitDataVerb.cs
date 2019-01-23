using CommandLine;
using MongoDB.ClusterMaintenance.Operations;
using Ninject;

namespace MongoDB.ClusterMaintenance.Verbs
{
	[Verb("presplit", HelpText = "distribute data by zones with splitting existing chunks")]
	public class PresplitDataVerb : BaseOptions
	{
		public override void BindOperation(IKernel kernel)
		{
			kernel.Bind<IOperation>().To<PresplitDataOperation>();
		}
	}
}
