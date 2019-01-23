using CommandLine;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.ClusterMaintenance.Config;
using NConfiguration;
using NConfiguration.Joining;
using NConfiguration.Xml;
using Ninject;
using NLog;

namespace MongoDB.ClusterMaintenance
{
	public abstract class BaseOptions
	{
		[Option('f', "config", Required = false, HelpText = "configuration file", Default = "configuration.xml")]
		public string ConfigFile { get; set; }
		
		[Option('d', "database", Required = false, HelpText = "database")]
		public string Database { get; set; }
		
		[Option('c', "collection", Required = false,  HelpText = "collection")]
		public string Collection { get; set; }

		public abstract void BindOperation(IKernel kernel);
	}
}
