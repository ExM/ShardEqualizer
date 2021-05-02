using System;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using Ninject;
using ShardEqualizer.ByteSizeRendering;
using ShardEqualizer.Operations;

namespace ShardEqualizer.Verbs
{
	[Verb("equalize", HelpText = "Align shard sizes by changing zone ranges")]
	public class EqualizeVerb: BaseCommandFileVerb
	{
		[Option("move-limit", Required = false, Default = null, HelpText = "Limit of data to move (Mb).")]
		public long? MoveLimit { get; set; }

		[Option("correction-percent", Required = false, Default = 100, HelpText = "Percentage of partial correction from 0 to 100.")]
        public double CorrectionPercent { get; set; }

		[Option("dry-run", Required = false, Default = false, HelpText = "Simulate equalization.")]
		public bool DryRun { get; set; }

		protected override Task RunOperation(IKernel kernel, CancellationToken token)
		{
			long? scaledMoveLimit = null;
			if (MoveLimit.HasValue)
				scaledMoveLimit = MoveLimit.Value * ScaleSuffix.Mega.Factor();

			if (CorrectionPercent <= 0 || CorrectionPercent > 100)
				throw new ArgumentException("correctionPercent must be in the range from 0 to 100 ");

			kernel.Bind<IOperation>().To<EqualizeOperation>()
				.WithConstructorArgument(scaledMoveLimit)
				.WithConstructorArgument(CorrectionPercent / 100)
				.WithConstructorArgument(DryRun);

			return base.RunOperation(kernel, token);
		}
	}
}
