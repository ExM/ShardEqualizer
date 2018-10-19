using MongoDB.Bson;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Events;
using NLog;

namespace MongoDB.ClusterMaintenance
{
	public sealed class CommandLogger
	{
		private static readonly Logger _log = LogManager.GetCurrentClassLogger();

		public static void Subscriber(ClusterBuilder clusterBuilder)
		{
			clusterBuilder.Subscribe<CommandStartedEvent>(onCommandStarted);
			clusterBuilder.Subscribe<CommandSucceededEvent>(onCommandSucceeded);
			clusterBuilder.Subscribe<CommandFailedEvent>(onCommandFailed);
		}

		private static void onCommandStarted(CommandStartedEvent ev)
		{
			if (!_log.IsTraceEnabled)
				return;

			_log.Trace($"Request id - {ev.RequestId}, operation id - {ev.OperationId}: {ev.DatabaseNamespace.DatabaseName} {ev.CommandName} {ev.Command.ToJson()}");
		}

		private static void onCommandSucceeded(CommandSucceededEvent ev)
		{
			if (!_log.IsTraceEnabled)
				return;

			_log.Trace($"Request id - {ev.RequestId}, operation id - {ev.OperationId}, duration - {ev.Duration}: {ev.CommandName} {ev.Reply.ToJson()}");
		}

		private static void onCommandFailed(CommandFailedEvent ev)
		{
			if (!_log.IsTraceEnabled)
				return;

			_log.Trace(ev.Failure, $"Request id - {ev.RequestId}, operation id - {ev.OperationId}, duration - {ev.Duration}: {ev.CommandName}");
		}
	}

}