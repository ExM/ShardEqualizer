using System;
using System.Globalization;

namespace MongoDB.ClusterMaintenance
{
	public class SizeRenderer
	{
		private readonly string _format;
		private readonly double _scale;

		public SizeRenderer(string format, ScaleSuffix scaleSuffix)
		{
			_format = format;
			_scale = Math.Pow(2, (int) scaleSuffix);
		}

		public string Render(long value)
		{
			return (value / _scale).ToString(_format, CultureInfo.InvariantCulture);
		}
	}
}