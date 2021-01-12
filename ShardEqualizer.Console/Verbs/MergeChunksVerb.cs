using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using MongoDB.ClusterMaintenance.MongoCommands;
using MongoDB.ClusterMaintenance.Operations;
using Ninject;
using NLog;

namespace MongoDB.ClusterMaintenance.Verbs
{
	[Verb("merge", HelpText = "Merge empty or small chunks")]
	public class MergeChunksVerb: BaseCommandFileVerb
	{
		public override Task RunOperation(IKernel kernel, CancellationToken token)
		{
			kernel.Bind<IOperation>().To<MergeChunksOperation>();

			return base.RunOperation(kernel, token);
		}
	}
}
