﻿using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using Ninject;
using NLog;
using ShardEqualizer.MongoCommands;

namespace ShardEqualizer.Verbs
{
	public class BaseCommandFileVerb: BaseVerbose
	{
		[Option("commandFile", Required = false, Default = "", HelpText = "file for command plan")]
		public string CommandFile { get; set; }
		
		public override async Task RunOperation(IKernel kernel, CancellationToken token)
		{
			if (string.IsNullOrWhiteSpace(CommandFile))
				CommandFile = $"commandPlan_{DateTime.UtcNow:yyyyMMdd_HHmm}.js";
			
			var fullPath = Path.GetFullPath(CommandFile);
			
			using (var file = File.CreateText(fullPath))
			{
				var commandPlanWriter = new CommandPlanWriter(file);
				kernel.Bind<CommandPlanWriter>().ToConstant(commandPlanWriter);
				
				await kernel.Get<IOperation>().Run(token);
			}
			
			_log.Info("all commands in file {0}", fullPath);
		}
		
		private static readonly Logger _log = LogManager.GetCurrentClassLogger();
	}
}
