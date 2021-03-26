using System;
using System.Runtime.Serialization;

namespace ShardEqualizer.Config
{
	[DataContract(Name = "LocalStore")]
	public class LocalStoreConfig
	{
		[DataMember(Name = "Clean")]
		public bool? Clean { get; set; }

		[DataMember(Name = "Read")]
		public bool? Read { get; set; }

		[DataMember(Name = "Write")]
		public bool? Write { get; set; }

		public void UpdateModes(string modeFlags)
		{
			if (modeFlags == null)
				return;

			Clean = modeFlags.Contains('c', StringComparison.OrdinalIgnoreCase);
			Read = modeFlags.Contains('r', StringComparison.OrdinalIgnoreCase);
			Write = modeFlags.Contains('w', StringComparison.OrdinalIgnoreCase);
		}
	}
}
