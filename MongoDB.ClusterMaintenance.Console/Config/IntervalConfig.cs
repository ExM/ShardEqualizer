using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using MongoDB.ClusterMaintenance.Models;
using MongoDB.Driver;

namespace MongoDB.ClusterMaintenance.Config
{
	[DataContract(Name = "interval")]
	public class IntervalConfig
	{
		[DataMember(Name = "nameSpace")]
		public string Namespace { get; set; }

		[DataMember(Name = "bounds")]
		public string Bounds { get; set; }

		[DataMember(Name = "zones")]
		public string Zones { get; set; }

		[DataMember(Name = "preSplit")]
		public PreSplitType PreSplit { get; private set; }

		[DataMember(Name = "correction")]
		public bool Correction { get; private set; }
	}
}