using System;
using System.Collections.Generic;
using System.Linq;

namespace ShardEqualizer.UI
{
	public interface IConsoleBookmark
	{
		void Clear();
		void Render(string line);
	}

	public class ConsoleBookmark: IConsoleBookmark
	{
		private readonly List<int> _renderedSymbols = new List<int>();
		private int _renderedLines = 0;
		private readonly int _startLeft;

		public ConsoleBookmark()
		{
			_startLeft = Console.CursorLeft;
		}

		public void Clear()
		{
			if (_renderedLines == 0)
				return;
			
			var currentTop = Console.CursorTop;
			var startTop = currentTop - _renderedLines + 1;
			
			Console.SetCursorPosition(_startLeft, startTop);

			Console.Write(new string(' ', _renderedSymbols.First()));
			foreach (var lineLength in _renderedSymbols.Skip(1))
			{
				Console.WriteLine();
				Console.Write(new string(' ', lineLength));
			}
			
			Console.SetCursorPosition(_startLeft, startTop);
			_renderedLines = 0;
			_renderedSymbols.Clear();
		}

		public void Render(string line)
		{
			if (_renderedSymbols.Count != 0)
				Console.WriteLine();
			
			Console.Write(line);
			_renderedLines += line.Length / Console.WindowWidth + 1;
			_renderedSymbols.Add(line.Length);
		}
	}
}
