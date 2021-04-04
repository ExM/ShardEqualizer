using System.Runtime.Serialization;

namespace ShardEqualizer.Config
{
	[DataContract(Name = "Defaults")]
	public class DefaultsConfig
	{
		[DataMember(Name = "Zones")]
		public string Zones { get; private set; }
	}
}
