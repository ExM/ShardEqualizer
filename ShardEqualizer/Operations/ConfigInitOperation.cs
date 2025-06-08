using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ShardEqualizer.Config;
using ShardEqualizer.ConfigServices;
using ShardEqualizer.Contracts.UI;
using ShardEqualizer.DAL.Models;
using ShardEqualizer.ScriptGen;
using ShardEqualizer.ShardedClusterViews;
using ShardEqualizer.Verbs;

namespace ShardEqualizer.Operations
{
	public class ConfigInitOperation: IOperation
	{
		private readonly string _configFileName;
		private readonly ConnectionConfig _connectionConfig;
		private readonly ShardsView _shardsView;
		private readonly ShardedCollectionInfoView _shardedCollectionInfoView;
		private readonly CommandPlanWriter _commandPlanWriter;
		private readonly IProgressCollector _progressRenderer;

		public ConfigInitOperation(
			BaseVerbose baseVerbose,
			ConnectionConfig connectionConfig,
			ShardsView shardsView,
			ShardedCollectionInfoView shardedCollectionInfoView,
			CommandPlanWriter commandPlanWriter,
			IProgressCollector progressRenderer)
		{
			_configFileName = baseVerbose.ConfigFile;
			_connectionConfig = connectionConfig;
			_shardsView = shardsView;
			_shardedCollectionInfoView = shardedCollectionInfoView;
			_commandPlanWriter = commandPlanWriter;
			_progressRenderer = progressRenderer;
		}

		public async Task Run(CancellationToken token)
		{
			var shards = await _shardsView.Get(token);
			var defaultZones = new List<string>(shards.Count);
			
			foreach (var shard in shards.OrderBy(s => s.Id.ToString(), StringComparer.Ordinal))
			{
				var defaultZoneName = shard.Id.ToString();
				defaultZones.Add(defaultZoneName);
				if(shard.HaveTag(new TagIdentity(defaultZoneName)))
					continue;
				_commandPlanWriter.AddShardToZone(shard.Id, defaultZoneName);
			}

			var shardedCollections = await _shardedCollectionInfoView.Get(token);

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
				DefaultZones = string.Join(",", defaultZones),
				ShardedCollections = shardedCollections.Values.Select(c => c.Id.ToString()).ToList(), //TODO exclude hashed keys
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
