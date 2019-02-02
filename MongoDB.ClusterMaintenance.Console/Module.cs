using System;
using MongoDB.ClusterMaintenance.Serialization;
using Ninject.Modules;

namespace MongoDB.ClusterMaintenance
{
	public class Module: NinjectModule
	{
		public override void Load()
		{
			CollectionNamespaceSerializer.Register();
			Bind<IConfigDbRepositoryProvider>().To<ConfigDbRepositoryProvider>().InSingletonScope();
			Bind<IAdminDB>().To<AdminDB>().InSingletonScope();
		}
	}
}