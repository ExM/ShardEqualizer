using CommandLine;
using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.ClusterMaintenance
{
	[Verb("merge", HelpText = "Merge empty or small chunks")]
	public class MergeChunks: BaseOptions
	{
		public override async Task Run(CancellationToken token)
		{
			//TODO
		}
	}
}
