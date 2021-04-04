using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using Ninject;
using NLog;
using ShardEqualizer.Verbs;

namespace ShardEqualizer
{
	internal static class Program
	{
		private static readonly Logger _log = LogManager.GetCurrentClassLogger();

		static async Task<int> Main(string[] args)
		{
			var cts = new CancellationTokenSource();

			Console.CancelKeyPress += (sender, eventArgs) =>
			{
				cts.Cancel();
				eventArgs.Cancel = true;
				_log.Warn("cancel operation requested...");
			};

			var parsed = Parser.Default.ParseArguments(args, loadVerbs()) as Parsed<object>;

			if (parsed == null)
				return 1;

			try
			{
				var verbose = (BaseVerbose) parsed.Value;

				var kernel = new StandardKernel(new NinjectSettings() { LoadExtensions = false });
				kernel.Bind<BaseVerbose>().ToConstant(verbose);
				kernel.Load<Module>();

				await verbose.Run(kernel, cts.Token);

				return 0;
			}
			catch (Exception e)
			{
				_log.Fatal(e, "unexpected exception");
				Console.Error.WriteLine();
				Console.Error.WriteLine(e.Message);
				return 1;
			}
			finally
			{
				LogManager.Flush();
			}
		}

		private	static Type[] loadVerbs() => Assembly.GetExecutingAssembly().GetTypes()
				.Where(t => t.GetCustomAttribute<VerbAttribute>() != null && !t.IsAbstract).ToArray();
	}
}
