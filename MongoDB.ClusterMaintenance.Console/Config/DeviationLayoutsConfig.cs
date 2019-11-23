using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MongoDB.ClusterMaintenance.Config
{
	[DataContract(Name = "DeviationLayouts")]
	public class DeviationLayoutsConfig
	{
		[DataMember(Name = "Layout")]
		public List<LayoutConfig> Layouts { get; private set; }
	}
}