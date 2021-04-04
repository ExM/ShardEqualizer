using System;
using System.Collections.Generic;
using ShardEqualizer.Config;

namespace ShardEqualizer.Reporting
{
	public class LayoutStore
	{
		private readonly Dictionary<string, LayoutDescription> _map = new Dictionary<string, LayoutDescription>(StringComparer.OrdinalIgnoreCase);

		public LayoutStore(List<LayoutConfig> layoutConfigs)
		{
			if(layoutConfigs != null)
				foreach (var layoutConfig in layoutConfigs)
				{
					if(_map.ContainsKey(layoutConfig.Name))
						throw new ArgumentException($"duplicate layout name {layoutConfig.Name} in configuration");

					_map.Add(layoutConfig.Name, new LayoutDescription(layoutConfig));
				}

			if(!_map.ContainsKey("default"))
				_map.Add("default", new LayoutDescription("Base report", "TtSz TtSt TtIs TtSzD TtStD TtIsD UsSz UsSt UsIs ShSz ShSt ShIs"));
			if(!_map.ContainsKey("balance"))
				_map.Add("balance", new LayoutDescription("Balance report", "MnSz MnSzD FxSz FxSzD"));
		}

		public IEnumerable<LayoutDescription> Get(IEnumerable<string> names)
		{
			foreach (var name in names)
			{
				if(!_map.TryGetValue(name, out var result))
					throw new Exception($"layout name {name} not found");

				yield return result;
			}
		}
	}
}
