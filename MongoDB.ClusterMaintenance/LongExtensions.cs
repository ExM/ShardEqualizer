using System;
using System.Globalization;

namespace MongoDB.ClusterMaintenance
{
	public static class LongExtensions
	{
		private static readonly string[] _sizeSuffixes = { "b", "Kb", "Mb", "Gb", "Tb", "Pb", "Eb" };

		public static string ByteSize(this long size)
		{
			if (size == 0)
				return "0 " + _sizeSuffixes[0];
			var uSize = Math.Abs(size);
			var dPlace = Math.Log(uSize, 1024);
			var placeDown = (int)Math.Floor(dPlace);
			var placeUp = (int)Math.Ceiling(dPlace);
			var scaledDown = uSize / Math.Pow(1024, placeDown);
			var scaledUp = uSize / Math.Pow(1024, placeUp);
			
			return scaledDown < 1000 
				? (Math.Sign(size) * scaledDown).ToString("0.##", CultureInfo.InvariantCulture) + " " + _sizeSuffixes[placeDown]
				: (Math.Sign(size) * scaledUp).ToString("0.##", CultureInfo.InvariantCulture) + " " + _sizeSuffixes[placeUp];
		}
	}
}