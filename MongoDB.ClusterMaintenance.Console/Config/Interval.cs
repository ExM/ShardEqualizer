using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using MongoDB.Driver;

namespace MongoDB.ClusterMaintenance.Config
{
	[DataContract(Name = "interval")]
	public class Interval
	{
		[DataMember(Name = "nameSpace")]
		private string _namespace
		{
			get => Namespace?.FullName;
			set => Namespace = CollectionNamespace.FromFullName(value);
		}
		
		[IgnoreDataMember]
		public CollectionNamespace Namespace { get; private set; }

		[DataMember(Name = "chunkFrom")]
		private string _chunkFrom { get; set; }

		[IgnoreDataMember]
		public string ChunkFrom => $"{Namespace.FullName}-{_chunkFrom}";
		
		[DataMember(Name = "chunkTo")]
		private string _chunkTo { get; set; }
		
		[IgnoreDataMember]
		public string ChunkTo => $"{Namespace.FullName}-{_chunkTo}";

		[DataMember(Name = "zones")]
		private string _zones
		{
			get => Zones == null ? null : string.Join(",", Zones);
			set => Zones = value?.Split(',').ToList();
		}
		
		[IgnoreDataMember]
		public IReadOnlyList<string> Zones { get; private set; }
		
		[DataMember(Name = "preSplit")]
		public PreSplitType PreSplit { get; private set; }

		[DataMember(Name = "correction")]
		public bool Correction { get; private set; }
	}
}