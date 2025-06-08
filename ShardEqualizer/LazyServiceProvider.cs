using Ninject;

namespace ShardEqualizer;

public class LazyServiceProvider: ILazyServiceProvider
{
    private readonly IKernel _kernel;

    public LazyServiceProvider(IKernel kernel)
    {
        _kernel = kernel;
    }
    
    public T Resolve<T>()
    {
        return _kernel.Get<T>();
    }
}