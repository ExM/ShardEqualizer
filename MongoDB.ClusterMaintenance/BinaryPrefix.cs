using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace MongoDB.ClusterMaintenance
{
	public static class BinaryPrefix
	{
		public static long Parse(string text)
		{
			if (text == null)
				throw new ArgumentNullException(nameof(text));
			if(string.IsNullOrWhiteSpace(text))
				throw new FormatException($"{nameof(text)} don't contain text");

			switch(Char.ToUpperInvariant(text.Last()))
			{
				case 'K':
					return long.Parse(text.Substring(0, text.Length - 1)) * 1024;
				case 'M':
					return long.Parse(text.Substring(0, text.Length - 1)) * 1024 * 1024;
				case 'G':
					return long.Parse(text.Substring(0, text.Length - 1)) * 1024 * 1024 * 1024;
				case 'T':
					return long.Parse(text.Substring(0, text.Length - 1)) * 1024 * 1024 * 1024 * 1024;

				default:
					return long.Parse(text);
			}
		}
	}
}
