using System.Runtime.Serialization;

namespace ShardEqualizer.Config
{
	[DataContract(Name = "Interval")]
	public class IntervalConfig
	{
		[DataMember(Name = "NameSpace")]
		public string Namespace { get; set; }

		[DataMember(Name = "MinBound")]
		public string MinBound { get; set; }

		[DataMember(Name = "MaxBound")]
		public string MaxBound { get; set; }

		[DataMember(Name = "Zones")]
		public string Zones { get; set; }

		[DataMember(Name = "Adjustable")]
		public bool? Adjustable { get; set; }
	}
}
