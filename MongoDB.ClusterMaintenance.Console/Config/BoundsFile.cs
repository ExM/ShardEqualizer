using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace MongoDB.ClusterMaintenance.Config
{
	[DataContract(Name = "BoundsFile")]
	public class BoundsFile
	{
		[DataMember(Name = "path")]
		public string Path { get; private set; }
	}
}