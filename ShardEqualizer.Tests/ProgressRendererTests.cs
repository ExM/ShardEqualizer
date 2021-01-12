using System;
using NUnit.Framework;

namespace ShardEqualizer
{
	[TestFixture]
	public class ProgressRendererTests
	{
		[Test]
		public void Demo()
		{
		
		
			Console.WriteLine(Console.IsOutputRedirected);
			
			
			Console.Write("123");

			//Console.CursorLeft = p;
			
			Console.Write("321");

			Console.Write("\b");

		}
	}
}