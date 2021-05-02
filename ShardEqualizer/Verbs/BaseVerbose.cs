using System;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using Ninject;
using NLog;

namespace ShardEqualizer.Verbs
{
	public abstract class BaseVerbose
	{
		[Option('c', "config", Required = false, HelpText = "The configuration file to use.", Default = "configuration.xml")]
		public string ConfigFile { get; set; }

		[Option("store-mode", Required = false,  HelpText = "Operation modes of the intermediate file storage [c - clean, r - read, w - write].")]
		public string StoreMode { get; set; }

		public async Task Run(IKernel kernel, CancellationToken token)
		{
			try
			{
				await RunOperation(kernel, token);
			}
			catch (Exception)
			{
				if (token.IsCancellationRequested)
					return;

				throw;
			}
			finally
			{
				foreach (var item in kernel.GetAll<IAsyncDisposable>())
					await item.DisposeAsync();

				LogManager.Flush();
			}
		}


		protected virtual async Task RunOperation(IKernel kernel, CancellationToken token)
		{
			await kernel.Get<IOperation>().Run(token);
		}
	}
}
