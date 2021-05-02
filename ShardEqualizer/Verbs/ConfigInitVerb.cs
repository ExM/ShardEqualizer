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
		[Option('h', "hosts", Required = true, HelpText = "Required. The host name (and optional port number) where the mongos of the sharded cluster is running. You can specify multiple hosts separated by commas.")]
		public string Hosts { get; set; }

		[Option('u', "user", Required = false, HelpText = "User name with which to authenticate to the mongos.")]
		public string User { get; set; }

		[Option('p', "password", Required = false, HelpText = "Password with which to authenticate to the mongos.")]
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
