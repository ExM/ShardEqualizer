using Ninject.Modules;

namespace MongoDB.ClusterMaintenance
{
	public class Module: NinjectModule
	{
		public override void Load()
		{
			Bind<IConfigDbRepositoryProvider>().To<ConfigDbRepositoryProvider>().InSingletonScope();
			Bind<IAdminDB>().To<AdminDB>().InSingletonScope();
		}
	}
}