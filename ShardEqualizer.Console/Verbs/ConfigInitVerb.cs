using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using Ninject;
using ShardEqualizer.Config;
using ShardEqualizer.Operations;

namespace ShardEqualizer.Verbs
{
	[Verb("config-init", HelpText = "Create new configuration.")]
	public class ConfigInitVerb: BaseCommandFileVerb
	{
		[Option('h', "hosts", Required = true, HelpText = "The host (and optional port number) where mongos instance for a sharded cluster is running. You can specify multiple hosts separated by commas")]
		public string Hosts { get; set; }

		[Option('u', "user", Required = false, HelpText = "user name for authentication")]
		public string User { get; set; }

		[Option('p', "password", Required = false, HelpText = "password for authentication")]
		public string Password { get; set; }

		protected override async Task RunOperation(IKernel kernel, CancellationToken token)
		{
			kernel.Rebind<ConnectionConfig>().ToConstant(new ConnectionConfig()
			{
				Servers = Hosts,
				User = User,
				Password = Password
			});

			var localStoreConfig = new LocalStoreConfig();
			localStoreConfig.UpdateModes(StoreMode);
			kernel.Rebind<LocalStoreConfig>().ToConstant(localStoreConfig);

			kernel.Bind<IOperation>().To<ConfigInitOperation>();

			await base.RunOperation(kernel, token);
		}
	}
}
