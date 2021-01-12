using System.Runtime.Serialization;

namespace ShardEqualizer.Config
{
	[DataContract(Name = "BoundsFile")]
	public class BoundsFile
	{
		[DataMember(Name = "path")]
		public string Path { get; private set; }
	}
}