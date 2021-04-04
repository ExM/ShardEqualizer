using System.Runtime.Serialization;

namespace ShardEqualizer.Config
{
	[DataContract(Name = "Connection")]
	public class ConnectionConfig
	{
		[DataMember(Name = "Servers")]
		public string Servers { get; set; }

		[DataMember(Name = "User")]
		public string User { get; set; }

		[DataMember(Name = "Password")]
		public string Password { get; set; }

		public bool IsRequireAuth => User != null || Password != null;
	}
}
