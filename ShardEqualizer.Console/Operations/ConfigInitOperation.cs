using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ShardEqualizer.Config;
using ShardEqualizer.MongoCommands;

namespace ShardEqualizer.Operations
{
	public class ConfigInitOperation: IOperation
	{
		private readonly string _configFileName;
		private readonly ConnectionConfig _connectionConfig;
		private readonly ShardListService _shardListService;
		private readonly ShardedCollectionService _shardedCollectionService;
		private readonly CommandPlanWriter _commandPlanWriter;
		private readonly ProgressRenderer _progressRenderer;

		public ConfigInitOperation(
			BaseVerbose baseVerbose,
			ConnectionConfig connectionConfig,
			ShardListService shardListService,
			ShardedCollectionService shardedCollectionService,
			CommandPlanWriter commandPlanWriter,
			ProgressRenderer progressRenderer)
		{
			_configFileName = baseVerbose.ConfigFile;
			_connectionConfig = connectionConfig;
			_shardListService = shardListService;
			_shardedCollectionService = shardedCollectionService;
			_commandPlanWriter = commandPlanWriter;
			_progressRenderer = progressRenderer;
		}

		public async Task Run(CancellationToken token)
		{
			var shards = await _shardListService.Get(token);
			//TODO validate existing tags
			var zones = shards.Select(_ => (shardId: _.Id, zoneName: _.Id.ToString())).OrderBy(_ => _.shardId.ToString(), StringComparer.Ordinal).ToList();

			foreach (var (shardId, zoneName) in zones)
				_commandPlanWriter.AddShardToZone(shardId, zoneName);

			var shardedCollections = await _shardedCollectionService.Get(token);

			string secretFileName = null;
			if (_connectionConfig.IsRequireAuth)
			{
				var configExt = Path.GetExtension(_configFileName);
				var configName = Path.GetFileNameWithoutExtension(_configFileName);

				secretFileName = $"{configName}.secret{configExt}";

				var fullLocalPath = Path.GetDirectoryName(Path.GetFullPath(_configFileName));

				var extConfigPath = Path.Combine(Path.GetDirectoryName(fullLocalPath), "ShardEqualizer.ExtConfigs");
				Directory.CreateDirectory(extConfigPath);

				var secretFileFullPath = Path.Combine(extConfigPath, secretFileName);

				_progressRenderer.WriteLine($"Create secret file: {secretFileFullPath}");

				await using var secretFile = File.CreateText(secretFileFullPath);

				var secretConfigRenderer = new SecretConfigRenderer()
				{
					User = _connectionConfig.User,
					Password = _connectionConfig.Password
				};

				secretConfigRenderer.WriteTo(secretFile);
			}

			var mainConfigRenderer = new MainConfigRenderer()
			{
				Servers = _connectionConfig.Servers,
				DefaultZones = string.Join(",", zones.Select(_ => _.zoneName)),
				ShardedCollections = shardedCollections.Values.Where(_ => !_.Dropped).Select(_ => _.Id.ToString()).ToList(), //TODO exclude hashed keys
				SecretFileName = secretFileName
			};

			_progressRenderer.WriteLine($"Create config file: {_configFileName}");
			await using var file = File.CreateText(_configFileName);

			mainConfigRenderer.WriteTo(file);
		}
	}

	public class SecretConfigRenderer
	{
		public string User { get; set; }
		public  string Password { get; set; }

		public void WriteTo(TextWriter writer)
		{
			writer.WriteLine("<Configuration>");

			var attributes = "";

			if (User != null)
				attributes += $" User=\"{User}\"";
			if (Password != null)
				attributes += $" Password=\"{Password}\"";

			writer.WriteLine($"\t<Connection{attributes} />");

			writer.WriteLine("</Configuration>");
		}
	}

	public class MainConfigRenderer
	{
		public string Servers { get; set; }
		public  string DefaultZones { get; set; }
		public  IEnumerable<string> ShardedCollections { get; set; }
		public  string SecretFileName { get; set; }

		public void WriteTo(TextWriter writer)
		{
			writer.WriteLine("<Configuration>");

			if (Servers != null)
			{
				writer.WriteLine($"\t<Connection Servers=\"{Servers}\" />");
				writer.WriteLine();
			}

			if (DefaultZones != null)
			{
				writer.WriteLine($"\t<Defaults zones=\"{DefaultZones}\" />");
				writer.WriteLine();
			}

			if (ShardedCollections != null)
			{
				foreach (var ns in ShardedCollections)
					writer.WriteLine($"\t<Interval nameSpace=\"{ns}\" />");
				writer.WriteLine();
			}

			if (SecretFileName != null)
			{
				writer.WriteLine($"\t<IncludeXmlFile path='ShardEqualizer.ExtConfigs/{SecretFileName}' search=\"all\" include=\"first\" required=\"true\"/>");
				writer.WriteLine();
			}

			writer.WriteLine("</Configuration>");
		}
	}
}
