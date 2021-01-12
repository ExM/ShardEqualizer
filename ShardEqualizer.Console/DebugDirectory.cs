using System;
using System.IO;
using ShardEqualizer.Config;

namespace ShardEqualizer
{
	public class DebugDirectory
	{
		private readonly bool _pathExists;
		private readonly string _path;
		private readonly DateTime _dateTime;

		public DebugDirectory(DebugDump config)
		{
			if (config == null)
			{
				Enable = false;
				return;
			}

			Enable = true;
			
			_path = config.Path;
			if (string.IsNullOrWhiteSpace(_path))
			{
				_pathExists = false;
			}
			else
			{
				_pathExists = true;
				Directory.CreateDirectory(_path);
			}
			
			_dateTime = DateTime.UtcNow;
		}

		public bool Enable { get; }

		public string GetFileName(string type, string ext)
		{
			var fileName = $"{type}_{_dateTime:yyyyMMdd_HHmm}.{ext}";
			return _pathExists 
				? Path.Combine(_path, fileName)
				: fileName;
		}
	}
}