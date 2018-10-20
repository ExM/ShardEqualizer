using CommandLine;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NLog;

namespace MongoDB.ClusterMaintenance
{
	public abstract class BaseOptions
	{
		[Option('h', "hosts", Separator=',', Min = 1, Required = true, HelpText = "list of hosts MongoDB cluster")]
		public IList<string> Hosts { get; set; }
		
		[Option('d', "database", Required = true, HelpText = "database")]
		public string Database { get; set; }
		
		[Option('c', "collection", Required = true,  HelpText = "collection")]
		public string Collection { get; set; }
		
		[Option('u', "user", Required = false, HelpText = "user")]
		public string User { get; set; }
		
		[Option('p', "pass", Required = false, HelpText = "password")]
		public string Password { get; set; }
		
		[Option('s', "shards", Separator=',', Required = false, HelpText = "list of shards")]
		public IList<string> ShardNames { get; set; }

		public abstract Task Run(CancellationToken token);
		
		private static readonly Logger _log = LogManager.GetCurrentClassLogger();
		private readonly Lazy<MongoClient> _lazyMongoClient;

		protected BaseOptions()
		{
			_lazyMongoClient = new Lazy<MongoClient>(createClient);
		}

		private MongoClient createClient()
		{
			_log.Info("Connecting to {0}", string.Join(",", Hosts));

			var urlBuilder = new MongoUrlBuilder()
			{
				Servers = Hosts.Select(MongoServerAddress.Parse),
			};

			if(IsRequireAuth)
			{
				urlBuilder.AuthenticationSource = "admin";
				urlBuilder.Username = User;
				urlBuilder.Password = Password;
			}
			
			var settings = MongoClientSettings.FromUrl(urlBuilder.ToMongoUrl());
			settings.ClusterConfigurator += CommandLogger.Subscriber;
			settings.ReadPreference = ReadPreference.SecondaryPreferred;

			return new MongoClient(settings);
		}

		protected bool IsRequireAuth => User != null || Password != null;

		protected MongoClient MongoClient => _lazyMongoClient.Value;
	}
}
