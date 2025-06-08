namespace ShardEqualizer;

public interface ILazyServiceProvider
{
    T Resolve<T>();
}