using CommandLine;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NLog;

namespace MongoDB.ClusterMaintenance
{
	internal static class Program
	{
		private static readonly Logger _log = LogManager.GetCurrentClassLogger();
		
		static int Main(string[] args)
		{
			var cts = new CancellationTokenSource();

			Console.CancelKeyPress += (sender, eventArgs) =>
			{
				cts.Cancel();
				eventArgs.Cancel = true;
				_log.Warn("cancel operation requested...");
			};
			
			var parsed = Parser.Default.ParseArguments<ScanChunks, MergeChunks, DistributeData>(args) as Parsed<object>;
			
			return parsed == null 
				? 1
				: ProcessVerbAndReturnExitCode(((BaseOptions) parsed.Value).Run, cts.Token).Result;
		}
		
		private static async Task<int> ProcessVerbAndReturnExitCode(Func<CancellationToken, Task> action, CancellationToken token)
		{
			try
			{
				await action(token);
				return 0;
			}
			catch (Exception e)
			{
				if (!token.IsCancellationRequested)
				{
					_log.Fatal(e, "unexpected exception");
					Console.Error.WriteLine(e.Message);
				}
				
				return 1;
			}
			finally
			{
				LogManager.Flush();
			}
		}
	}
}
