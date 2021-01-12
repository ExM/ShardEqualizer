using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.ClusterMaintenance.Config;
using MongoDB.ClusterMaintenance.Models;
using MongoDB.Driver;

namespace MongoDB.ClusterMaintenance
{
	public class Interval
	{
		

		public Interval(CollectionNamespace ns, IReadOnlyList<TagIdentity> zones, PreSplitMode preSplit,
			CorrectionMode correction, BsonBound min, BsonBound max, double priority)
		{
			Namespace = ns;
			Zones = zones;
			PreSplit = preSplit;
			Correction = correction;
			Priority = priority;
			Min = min;
			Max = max;
		}
		
		public Interval(IntervalConfig config, BsonDocument bounds)
		{
			Selected = true;
			Namespace = CollectionNamespace.FromFullName(config.Namespace);;
			Zones = config.Zones?.Split(',').Select(_ => new TagIdentity(_)).ToList();;
			PreSplit = config.PreSplit ?? PreSplitMode.Auto;
			Correction = config.Correction ?? CorrectionMode.UnShard;
			Priority = config.Priority ?? 1;
			
			if (!string.IsNullOrWhiteSpace(config.Bounds))
			{
				var b = bounds[config.Bounds];
				Min = (BsonBound)b["min"].AsBsonDocument;
				Max = (BsonBound)b["max"].AsBsonDocument;
			}
		}

		public CollectionNamespace Namespace { get; }
		public BsonBound? Min { get; }
		public BsonBound? Max { get; }
		public IReadOnlyList<TagIdentity> Zones { get; }
		public PreSplitMode PreSplit { get; }
		public CorrectionMode Correction { get; }
		public double Priority { get; }
		public bool Selected { get; set; }
	}
}