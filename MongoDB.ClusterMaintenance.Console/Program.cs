using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.ClusterMaintenance.Config;
using MongoDB.ClusterMaintenance.Verbs;
using MongoDB.Driver;
using NConfiguration;
using NConfiguration.Joining;
using NConfiguration.Xml;
using NLog;
using Ninject;

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
			
			var parsed = Parser.Default.ParseArguments<ScanChunksVerb, MergeChunksVerb, PresplitDataVerb>(args) as Parsed<object>;

			if (parsed == null)
				return 1;

			var options = (BaseOptions) parsed.Value;

			var kernel = new StandardKernel(new NinjectSettings() { LoadExtensions = false });
			kernel.Load<Module>();
			BindConfiguration(options, kernel);
			options.BindOperation(kernel);

			var operation = kernel.Get<IOperation>();
			
			return ProcessVerbAndReturnExitCode(operation.Run, cts.Token).Result;
		}
		
		private static IAppSettings loadConfiguration(string configFile)
		{
			var loader = new SettingsLoader();
			loader.Loaded += (s, e) => _log.Info("Loaded: {0} ({1})", e.Settings.GetType(), e.Settings.Identity);
			loader.XmlFileBySection().FindingSettings += (s, e) => _log.Info("Search '{0}' from '{1}'", e.IncludeFile.Path, e.SearchPath);
			return loader.LoadSettings(new XmlFileSettings(configFile)).Joined.ToAppSettings();
		}

		public static void BindConfiguration(BaseOptions options, IKernel kernel)
		{
			var appSettings = loadConfiguration(options.ConfigFile);
			
			var connectionConfig = appSettings.Get<Connection>();
			kernel.Bind<IMongoClient>().ToMethod(_ => createClient(connectionConfig)).InSingletonScope();

			var intervals = appSettings.LoadSections<Interval>();
			
			if (options.Database != null)
				intervals = intervals.Where(_ =>
					_.Namespace.DatabaseNamespace.DatabaseName.Equals(options.Database, StringComparison.Ordinal));
			
			if(options.Collection != null)
				intervals = intervals.Where(_ =>
					_.Namespace.CollectionName.Equals(options.Collection, StringComparison.Ordinal));

			IReadOnlyList<Interval> finalIntervals = intervals.ToList();

			if (finalIntervals.Count == 0)
				throw new ArgumentException("interval list is empty");
			
			kernel.Bind<IReadOnlyList<Interval>>().ToConstant(finalIntervals);
		}
		
		private static IMongoClient createClient(Connection connectionConfig)
		{
			_log.Info("Connecting to {0}", string.Join(",", connectionConfig.Servers));

			var urlBuilder = new MongoUrlBuilder()
			{
				Servers = connectionConfig.Servers.Select(MongoServerAddress.Parse),
			};

			if(connectionConfig.IsRequireAuth)
			{
				urlBuilder.AuthenticationSource = "admin";
				urlBuilder.Username = connectionConfig.User;
				urlBuilder.Password = connectionConfig.Password;
			}
			
			var settings = MongoClientSettings.FromUrl(urlBuilder.ToMongoUrl());
			settings.ClusterConfigurator += CommandLogger.Subscriber;
			settings.ReadPreference = ReadPreference.Primary;

			return new MongoClient(settings);
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
