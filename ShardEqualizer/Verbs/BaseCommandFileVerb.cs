using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using Ninject;
using NLog;
using ShardEqualizer.MongoCommands;
using ShardEqualizer.UI;

namespace ShardEqualizer.Verbs
{
	public abstract class BaseCommandFileVerb: BaseVerbose
	{
		[Option("script-file", Required = false, HelpText = "(Default: commandPlan_{currentDateTime}.js) Command plan file.")]
		public string ScriptFile { get; set; }

		protected override async Task RunOperation(IKernel kernel, CancellationToken token)
		{
			if (string.IsNullOrWhiteSpace(ScriptFile))
				ScriptFile = $"commandPlan_{DateTime.UtcNow:yyyyMMdd_HHmm}.js";

			var fullPath = Path.GetFullPath(ScriptFile);

			await using (var file = File.CreateText(fullPath))
			{
				var commandPlanWriter = new CommandPlanWriter(file);
				kernel.Bind<CommandPlanWriter>().ToConstant(commandPlanWriter);

				await base.RunOperation(kernel, token);
			}

			kernel.Get<ProgressRenderer>().WriteLine($"save all commands to file {fullPath}");
			_log.Info("all commands in file {0}", fullPath);
		}

		private static readonly Logger _log = LogManager.GetCurrentClassLogger();
	}
}
