using System.Runtime.Serialization;

namespace ShardEqualizer.Config
{
	public class LayoutConfig
	{
		[DataMember(Name = "Name")]
		public string Name { get; private set; }
		
		[DataMember(Name = "Title")]
		public string Title { get; private set; }
		
		[DataMember(Name = "Columns")]
		public string Columns { get; private set; }

		public static LayoutConfig Default => new LayoutConfig()
		{
			Name = "default",
			Title = "Data allocation of shards",
			Columns = "TtSz TtSt TtIs TtSzD TtStD TtIsD UsSz UsSt UsIs ShSz ShSt ShIs"
		};
	}
}