using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NLog;

namespace MongoDB.ClusterMaintenance
{
	public class ProgressReporter
	{
		private static readonly Logger _log = LogManager.GetCurrentClassLogger();
		private long _processed;
		private readonly long _total;
		private readonly Action _onShowProgress;
		private readonly CancellationTokenSource _cts;
		private readonly Task _loop;
		private readonly Stopwatch _sw;

		public ProgressReporter(long total, Action onShowProgress = null)
		{
			_total = total;
			_onShowProgress = onShowProgress;
			_cts = new CancellationTokenSource();
			_log.Info("Total elements: {0}", _total);
			_sw = Stopwatch.StartNew();
			_loop = showProgressLoop();
		}

		public void Increment()
		{
			Interlocked.Increment(ref _processed);
		}
		
		private async Task showProgressLoop()
		{
			while (showProgress())
				await Task.Delay(TimeSpan.FromSeconds(1), _cts.Token);
		}

		private bool showProgress()
		{
			var copyProcessed = Interlocked.Read(ref _processed);

			var elapsed = _sw.Elapsed;
			var percent = (double)copyProcessed / _total;
			var s = percent <= 0 ? 0 : (1 - percent) / percent;

			var eta = TimeSpan.FromSeconds(elapsed.TotalSeconds * s);

			_log.Info("Progress {0}/{1} Elapsed: {2} ETA: {3}", copyProcessed, _total, elapsed, eta);

			_onShowProgress?.Invoke();
			
			return copyProcessed < _total;
		}

		public async Task Finalize()
		{
			_cts.Cancel();
			try
			{
				await _loop;
			}
			catch (TaskCanceledException)
			{
			}
			
			Interlocked.Exchange(ref _processed, _total);
			showProgress();
		}
	}
}