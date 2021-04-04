using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Driver;
using NConfiguration;
using NConfiguration.Combination;
using NConfiguration.Joining;
using NConfiguration.Variables;
using NConfiguration.Xml;
using Ninject;
using Ninject.Activation;
using Ninject.Modules;
using NLog;
using ShardEqualizer.Config;
using ShardEqualizer.ConfigRepositories;
using ShardEqualizer.ConfigServices;
using ShardEqualizer.LocalStoring;
using ShardEqualizer.Reporting;
using ShardEqualizer.Serialization;
using ShardEqualizer.UI;
using ShardEqualizer.Verbs;

namespace ShardEqualizer
{
	public class Module: NinjectModule
	{
		private static readonly Logger _log = LogManager.GetCurrentClassLogger();

		public override void Load()
		{
			CollectionNamespaceSerializer.Register();
			Bind<MongoClientBuilder>().ToSelf().InSingletonScope();
			Bind<IAsyncDisposable, ProgressRenderer>().To<ProgressRenderer>().InSingletonScope();

			Bind<IMongoClient>().ToMethod(ctx => ctx.Kernel.Get<MongoClientBuilder>().Build()).InSingletonScope();
			Bind<ConfigDBContainer>().ToMethod(ctx => new ConfigDBContainer(ctx.Kernel.Get<IMongoClient>())).InSingletonScope();

			Bind<ChunkRepository>().ToSelf().InSingletonScope()
				.WithConstructorArgument(ctx => ctx.Kernel.Get<ConfigDBContainer>().MongoDatabase);
			Bind<CollectionRepository>().ToSelf().InSingletonScope()
				.WithConstructorArgument(ctx => ctx.Kernel.Get<ConfigDBContainer>().MongoDatabase);
			Bind<TagRangeRepository>().ToSelf().InSingletonScope()
				.WithConstructorArgument(ctx => ctx.Kernel.Get<ConfigDBContainer>().MongoDatabase);
			Bind<SettingsRepository>().ToSelf().InSingletonScope()
				.WithConstructorArgument(ctx => ctx.Kernel.Get<ConfigDBContainer>().MongoDatabase);
			Bind<ShardRepository>().ToSelf().InSingletonScope()
				.WithConstructorArgument(ctx => ctx.Kernel.Get<ConfigDBContainer>().MongoDatabase);
			Bind<VersionRepository>().ToSelf().InSingletonScope()
				.WithConstructorArgument(ctx => ctx.Kernel.Get<ConfigDBContainer>().MongoDatabase);

			Bind<IAdminDB>().To<AdminDB>().InSingletonScope();

			Bind<ShardedCollectionService>().ToSelf().InSingletonScope();
			Bind<TagRangeService>().ToSelf().InSingletonScope();
			Bind<ClusterSettingsService>().ToSelf().InSingletonScope();
			Bind<ShardListService>().ToSelf().InSingletonScope();
			Bind<CollectionListService>().ToSelf().InSingletonScope();
			Bind<CollectionStatisticService>().ToSelf().InSingletonScope();
			Bind<ChunkService>().ToSelf().InSingletonScope();
			Bind<ChunkSizeService>().ToSelf().InSingletonScope();

			Bind<LocalStoreProvider>().ToSelf().InSingletonScope();

			Bind<IAppSettings>().ToMethod(loadConfiguration).InSingletonScope();

			Bind<IReadOnlyList<Interval>>().ToMethod(loadIntervals).InSingletonScope();

			Bind<ConnectionConfig>()
				.ToMethod(ctx => ctx.Kernel.Get<IAppSettings>().Get<ConnectionConfig>())
				.InSingletonScope();

			Bind<LocalStoreConfig>().ToMethod(buildLocalStoreConfig).InSingletonScope();

			Bind<LayoutStore>()
				.ToMethod(ctx => new LayoutStore(ctx.Kernel.Get<IAppSettings>().TryGet<DeviationLayoutsConfig>()?.Layouts))
				.InSingletonScope();
		}

		private static LocalStoreConfig buildLocalStoreConfig(IContext ctx)
		{
			var appSettings = ctx.Kernel.Get<IAppSettings>();
			var storeMode = ctx.Kernel.Get<BaseVerbose>().StoreMode;

			var localStoreConfig = appSettings.TryGet<LocalStoreConfig>() ?? new LocalStoreConfig();
			localStoreConfig.UpdateModes(storeMode);

			return localStoreConfig;
		}

		private static IReadOnlyList<Interval> loadIntervals(IContext ctx)
		{
			return loadIntervalConfigurations(ctx.Kernel.Get<IAppSettings>())
				.Select(_ => new Interval(_)).ToList().AsReadOnly();
		}

		private static IEnumerable<IntervalConfig> loadIntervalConfigurations(IAppSettings appSettings)
		{
			var defaultZones = appSettings.TryGet<DefaultsConfig>()?.Zones;

			foreach (var g in appSettings.LoadSections<IntervalConfig>().GroupBy(_ => _.Namespace, StringComparer.Ordinal))
			{
				var intervalConfig = g.Aggregate(appSettings.Combine);
				intervalConfig.Zones ??= defaultZones;

				yield return intervalConfig;
			}
		}

		private static IAppSettings loadConfiguration(IContext ctx)
		{
			var configFile = ctx.Kernel.Get<BaseVerbose>().ConfigFile;

			var storage = new VariableStorage();
			var loader = new SettingsLoader(storage.CfgNodeConverter);
			loader.Loaded += (s, e) => _log.Info("Loaded: {0} ({1})", e.Settings.GetType(), e.Settings.Identity);
			loader.XmlFileBySection().FindingSettings += (s, e) => _log.Info("Search '{0}' from '{1}'", e.IncludeFile.Path, e.SearchPath);
			return loader.LoadSettings(new XmlFileSettings(configFile)).Joined.ToAppSettings();
		}
	}

	public class ConfigDBContainer
	{
		public ConfigDBContainer(IMongoClient mongoClient)
		{
			MongoDatabase = mongoClient.GetDatabase("config");
		}

		public IMongoDatabase MongoDatabase { get; }
	}
}
