using CommandLine;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NLog;

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
			
			var parsed = Parser.Default.ParseArguments<ScanChunks, MergeChunks>(args) as Parsed<object>;
			
			return parsed == null 
				? 1
				: ProcessOptionsAndReturnExitCode(((BaseOptions) parsed.Value).Run, cts.Token).Result;
		}
		
		private static async Task<int> ProcessOptionsAndReturnExitCode(Func<CancellationToken, Task> action, CancellationToken token)
		{
			try
			{
				await action(token);
				LogManager.Flush();
				return 0;
			}
			catch (Exception e)
			{
				if (!token.IsCancellationRequested)
				{
					_log.Fatal(e, "unexpected exception");
					Console.Error.WriteLine(e.Message);
				}
				LogManager.Flush();
				return 1;
			}
		}
	}

	[Verb("merge", HelpText = "Merge empty or small chunks")]
	public class MergeChunks: BaseOptions
	{
		public override async Task Run(CancellationToken token)
		{
			//TODO
		}
	}
	
	[Verb("scan", HelpText = "Scan chunks")]
	public class ScanChunks: BaseOptions
	{
		public override async Task Run(CancellationToken token)
		{
			var collRepo = new CollectionRepository(MongoClient);
			var chunkRepo = new ChunkRepository(MongoClient);

			var db = MongoClient.GetDatabase(Database);
			
			var collInfo = await collRepo.Find(Database, Collection);

			if (collInfo == null)
				throw new InvalidOperationException($"collection {Database}.{Collection} not sharded");

			var scanner = new EmptyChunkScanner(db, collInfo, chunkRepo, ShardNames, token);

			await scanner.Run();
		}
	}

	public abstract class BaseOptions
	{
		[Option('h', "hosts", Separator=',', Min = 1, Required = true, HelpText = "list of hosts MongoDB cluster")]
		public IList<string> Hosts { get; set; }
		
		[Option('d', "database", Required = true, HelpText = "database")]
		public string Database { get; set; }
		
		[Option('c', "collection", Required = true, HelpText = "collection")]
		public string Collection { get; set; }
		
		[Option('u', "user", Required = true, HelpText = "user")]
		public string User { get; set; }
		
		[Option('p', "pass", Required = true, HelpText = "password")]
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

			var url = new MongoUrlBuilder()
			{
				Servers = Hosts.Select(MongoServerAddress.Parse),
				AuthenticationSource = "admin",
				Username = User,
				Password = Password
			}.ToMongoUrl();
			
			var settings = MongoClientSettings.FromUrl(url);
			settings.ClusterConfigurator += CommandLogger.Subscriber;
			settings.ReadPreference = ReadPreference.SecondaryPreferred;

			return new MongoClient(settings);
		}

		protected MongoClient MongoClient => _lazyMongoClient.Value;
	}
}
