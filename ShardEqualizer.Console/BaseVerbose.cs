using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using Ninject;

namespace ShardEqualizer
{
	public abstract class BaseVerbose
	{
		[Option('f', "config", Required = false, HelpText = "configuration file", Default = "configuration.xml")]
		public string ConfigFile { get; set; }
		
		[Option('d', "database", Required = false, HelpText = "database")]
		public string Database { get; set; }
		
		[Option('c', "collection", Required = false,  HelpText = "collection")]
		public string Collection { get; set; }

		public abstract Task RunOperation(IKernel kernel, CancellationToken token);
	}
}
