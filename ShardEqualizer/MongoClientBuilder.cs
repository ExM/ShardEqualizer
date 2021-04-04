using System.Linq;
using MongoDB.Driver;
using NLog;
using ShardEqualizer.Config;

namespace ShardEqualizer
{
	public class MongoClientBuilder
	{
		private static readonly Logger _log = LogManager.GetCurrentClassLogger();
		private readonly ConnectionConfig _connectionConfig;
		private readonly ProgressRenderer _progressRenderer;

		public MongoClientBuilder(ConnectionConfig connectionConfig, ProgressRenderer progressRenderer)
		{
			_connectionConfig = connectionConfig;
			_progressRenderer = progressRenderer;
		}

		public IMongoClient Build()
		{
			_progressRenderer.WriteLine($"Connecting to {_connectionConfig.Servers}");
			_log.Info("Connecting to {0}", _connectionConfig.Servers);

			var urlBuilder = new MongoUrlBuilder()
			{
				Servers = _connectionConfig.Servers.Split(',').Select(MongoServerAddress.Parse).ToList(),
			};

			if(_connectionConfig.IsRequireAuth)
			{
				urlBuilder.AuthenticationSource = "admin";
				urlBuilder.Username = _connectionConfig.User;
				urlBuilder.Password = _connectionConfig.Password;
			}

			var settings = MongoClientSettings.FromUrl(urlBuilder.ToMongoUrl());
			settings.ClusterConfigurator += CommandLogger.Subscriber;
			settings.ReadPreference = ReadPreference.SecondaryPreferred;
			settings.MinConnectionPoolSize = 32;
			settings.ApplicationName = GetType().Assembly.GetName().ToString();
			return new MongoClient(settings);
		}
	}
}
