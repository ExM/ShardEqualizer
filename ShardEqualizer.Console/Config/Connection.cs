using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace MongoDB.ClusterMaintenance.Config
{
	[DataContract(Name = "Connection")]
	public class Connection
	{
		[DataMember(Name = "Servers")]
		private string _servers
		{
			get => Servers == null ? null : string.Join(",", Servers);
			set => Servers = value?.Split(',').ToList();
		}
		
		[IgnoreDataMember]
		public IReadOnlyList<string> Servers { get; private set; }
		
		[DataMember(Name = "User")]
		public string User { get; private set; }
		
		[DataMember(Name = "Password")]
		public string Password { get; private set; }
		
		public bool IsRequireAuth => User != null || Password != null;
	}
}