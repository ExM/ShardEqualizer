using ShardEqualizer.DAL;
using ShardEqualizer.DAL.Serialization;
using Xunit.v3;

[assembly: Xunit.TestFramework(typeof(AssemblyFixture))]

namespace ShardEqualizer.DAL;

public sealed class AssemblyFixture : XunitTestFramework
{
    public AssemblyFixture()
    {
        CommonSerializers.Register();
    }
}
