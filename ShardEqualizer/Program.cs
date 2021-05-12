using System;
using System.IO;
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
		private static readonly CancellationTokenSource _cts = new ();

		public static async Task<int> Main(string[] args)
		{
			try
			{
				LogManager.Configuration = null;
				var nlogConfigFile = Path.Combine(Environment.CurrentDirectory, "NLog.config");
				if(File.Exists(nlogConfigFile)) //use config file from current directory
					LogManager.LoadConfiguration(nlogConfigFile);
			}
			catch (Exception e)
			{
				Console.Error.WriteLine(e);
				return 1;
			}

			try
			{
				var parsed = Parser.Default.ParseArguments(args, loadVerbs()) as Parsed<object>;

				if (parsed == null)
					return 0;

				var verbose = (BaseVerbose) parsed.Value;

				var kernel = new StandardKernel(new NinjectSettings() { LoadExtensions = false });
				kernel.Bind<BaseVerbose>().ToConstant(verbose);
				kernel.Load<Module>();

				Console.CancelKeyPress += OnCancelKeyPress;
				await verbose.Run(kernel, _cts.Token);

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

		private static void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs eventArgs)
		{
			_cts.Cancel();
			eventArgs.Cancel = true;
			_log.Warn("cancel operation requested...");
		}

		private	static Type[] loadVerbs() => Assembly.GetExecutingAssembly().GetTypes()
				.Where(t => t.GetCustomAttribute<VerbAttribute>() != null && !t.IsAbstract).ToArray();
	}
}
