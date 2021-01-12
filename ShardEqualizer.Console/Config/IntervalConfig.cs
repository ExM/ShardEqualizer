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
		public PreSplitMode? PreSplit { get; private set; }

		[DataMember(Name = "correction")]
		public CorrectionMode? Correction { get; private set; }
		
		[DataMember(Name = "Priority")]
		public double? Priority { get; private set;}
	}
}