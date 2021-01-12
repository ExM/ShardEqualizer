using System.Runtime.Serialization;

namespace ShardEqualizer.Config
{
	[DataContract(Name = "DebugDump")]
	public class DebugDump
	{
		[DataMember(Name = "path")]
		public string Path { get; private set; }
	}
}