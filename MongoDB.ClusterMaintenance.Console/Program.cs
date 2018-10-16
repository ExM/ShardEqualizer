using CommandLine;
using MongoDB.Driver;
using System;
using System.Collections.Generic;

namespace MongoDB.ClusterMaintenance
{
	class Program
	{
		static void Main(string[] args)
		{
			Parser.Default.ParseArguments<Options>(args)
				.WithParsed<Options>(opts => RunOptionsAndReturnExitCode(opts));
		}

		private static void RunOptionsAndReturnExitCode(Options opts)
		{
			var client = new MongoClient(opts.ConnectionString);

			foreach (var name in client.ListDatabaseNames().ToList())
			{
				Console.WriteLine(name);
			}
		}
	}

	class Options
	{
		[Option('c', "connection", Required = true, HelpText = "connection string to MongoDB cluster")]
		public string ConnectionString { get; set; }
	}
}
