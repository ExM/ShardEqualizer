using CommandLine;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
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
				_log.Warn("canceled operation requested");
			};
			
			var resultCode = 1;
			Parser.Default.ParseArguments<Options>(args)
				.WithParsed(opts => { resultCode = ProcessOptionsAndReturnExitCode(opts, cts.Token).Result; });

			LogManager.Flush();
			return resultCode;
		}
		
		private static async Task<int> ProcessOptionsAndReturnExitCode(Options opts, CancellationToken token)
		{
			try
			{
				await ProcessOptions(opts, token);
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
		}

		private static async Task ProcessOptions(Options opts, CancellationToken token)
		{
			_log.Info("Connecting to {0}", opts.ConnectionString);
			var settings = MongoClientSettings.FromConnectionString(opts.ConnectionString);
			settings.ClusterConfigurator += CommandLogger.Subscriber;
			settings.ReadPreference = ReadPreference.SecondaryPreferred;

			var client = new MongoClient(settings);

			var collRepo = new CollectionRepository(client);
			var chunkRepo = new ChunkRepository(client);

			var db = client.GetDatabase(opts.Database);
			
			var collInfo = await collRepo.Find(opts.Database, opts.Collection);

			if (collInfo == null)
				throw new InvalidOperationException($"collection {opts.Database}.{opts.Collection} not sharded");

			var total = await chunkRepo.Count(opts.Database, opts.Collection);

			_log.Info("Total shards: {0}", total);
			
			var cursor = await chunkRepo.Find(opts.Database, opts.Collection);
			await cursor.ForEachAsync(chunk => processChunk(db, collInfo, chunk, token), token);
		}

		private static async Task processChunk(IMongoDatabase db, ShardedCollectionInfo collInfo, ChunkInfo chunk, CancellationToken token)
		{
			_log.Debug("Process chunk: {0}/{1}", chunk.Id, chunk.Shard);
			
			var cmd = new BsonDocument
			{
				{ "datasize", collInfo.Id },
				{ "keyPattern", collInfo.Key },
				{ "min", chunk.Min },
				{ "max", chunk.Max }
			};

			var result = await db.RunCommandAsync<DatasizeResult>(cmd, null, token).ConfigureAwait(false);
			
			if(result.IsSuccess)
				_log.Info("chunk: {0}/{1} size: {2}", chunk.Id, chunk.Shard, result.Size);
			else
				_log.Warn("datasize command fail");
		}
	}
	
	public class DatasizeResult : CommandResult
	{
		[BsonElement("size"), BsonIgnoreIfNull]
		public long Size { get; private set; }
		
		[BsonElement("numObjects"), BsonIgnoreIfNull]
		public long NumObjects { get; private set; }
		
		[BsonElement("millis"), BsonIgnoreIfNull]
		public long Millis { get; private set; }
	}


	class Options
	{
		[ValueAttribute(0, Required = true, HelpText = "connection string to MongoDB cluster")]
		public string ConnectionString { get; set; }
		
		[Option('d', "database", Required = true, HelpText = "database")]
		public string Database { get; set; }
		
		[Option('c', "collection", Required = true, HelpText = "database")]
		public string Collection { get; set; }
	}
}
