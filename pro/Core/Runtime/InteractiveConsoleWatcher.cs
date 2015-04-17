﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Peach.Core;

namespace Peach.Pro.Core.Runtime
{
	public class InteractiveConsoleWatcher : Watcher
	{
		readonly string _title = "Peach Pro v";
		private const string _copyright = "Copyright (c) Deja vu Security";

		readonly Stopwatch timer = new Stopwatch();
		uint startIteration;
		bool reproducing;

		RunContext _context;
		uint _currentIteration;
		uint _totalIterations;
		readonly List<Fault> _faults = new List<Fault>();
		readonly Dictionary<string, int> _majorFaultCount = new Dictionary<string, int>();
		DateTime _started = DateTime.Now;
		string _status = "";
		string _eta = "";
		DateTime _lastScreenUpdate = DateTime.Now;

		public InteractiveConsoleWatcher(string titleSuffix = "")
		{
			_title += Assembly.GetExecutingAssembly().GetName().Version;
			_title += titleSuffix;
		}

		protected override void Engine_ReproFault(RunContext context, uint currentIteration, Peach.Core.Dom.StateModel stateModel, Fault[] faultData)
		{
			_status = string.Format("Caught fault at iteration {0}, trying to reproduce", currentIteration);
			reproducing = true;

			RefreshScreen();
		}

		protected override void Engine_ReproFailed(RunContext context, uint currentIteration)
		{
			_status = string.Format("Could not reproduce fault at iteration {0}", currentIteration);
			reproducing = false;

			RefreshScreen();
		}

		protected override void Engine_Fault(RunContext context,
			uint currentIteration, Peach.Core.Dom.StateModel stateModel, Fault[] faultData)
		{
			_status = string.Format("{1} fault at iteration {0}", currentIteration,
				reproducing ? "Reproduced" : "Caught");

			reproducing = false;

			foreach (Fault fault in faultData)
			{
				if (fault.type == FaultType.Fault)
				{
					_faults.Add(fault);

					if (!string.IsNullOrEmpty(fault.majorHash))
					{
						if (_majorFaultCount.ContainsKey(fault.majorHash))
							_majorFaultCount[fault.majorHash] += 1;
						else
							_majorFaultCount[fault.majorHash] = 1;
					}
				}
			}

			RefreshScreen();
		}

		protected override void Engine_HaveCount(RunContext context, uint totalIterations)
		{
			_totalIterations = totalIterations;
			_context = context;
		}

		protected override void Engine_HaveParallel(RunContext context, uint startIteration, uint stopIteration)
		{
			_context = context;
		}

		protected override void Engine_IterationFinished(RunContext context, uint currentIteration)
		{
		}

		protected override void Engine_IterationStarting(RunContext context, uint currentIteration, uint? totalIterations)
		{
			_currentIteration = currentIteration;
			if (totalIterations != null)
				_totalIterations = totalIterations.Value;

			if (!timer.IsRunning)
			{
				timer.Start();
				startIteration = currentIteration;
			}

			if (totalIterations != null && totalIterations < uint.MaxValue)
			{
				var done = currentIteration - startIteration;
				var total = totalIterations.Value - startIteration + 1;
				var elapsed = timer.ElapsedMilliseconds;
				TimeSpan remain;

				if (done == 0)
				{
					remain = TimeSpan.FromMilliseconds(elapsed * total);
				}
				else
				{
					remain = TimeSpan.FromMilliseconds((total * elapsed / done) - elapsed);
				}

				_eta = remain.ToString("g");
			}


			if (!reproducing)
			{
				if (context.controlIteration && context.controlRecordingIteration)
					_status = "Recording iteration";
				else if (context.controlIteration)
					_status = "Control iteration";
				else
					_status = "Fuzzing iteration";
			}

			RefreshIterationAndStatus();
		}

		protected override void Engine_TestError(RunContext context, Exception e)
		{
			_status = "Test '" + context.test.Name + "' error: " + e.Message;
			RefreshScreen();
		}

		protected override void Engine_TestFinished(RunContext context)
		{
			_status = "Test '" + context.test.Name + "' finished.";
			RefreshScreen();
			Console.WriteLine();
			Console.WriteLine();
		}

		protected override void Engine_TestStarting(RunContext context)
		{
			_context = context;

			if (context.config.countOnly)
				_status = "Calculating total iterations by running single iteration.";

			RefreshScreen();
		}

		void DisplayStaticText(string text)
		{
			DisplayText(text, ConsoleColor.DarkCyan);
		}

		void DisplayText(string text, ConsoleColor color)
		{
			Console.ForegroundColor = color;
			Console.Write(text);
			Console.ForegroundColor = ConsoleColor.Gray;
		}

		public static void ClearCurrentConsoleLine()
		{
			int currentLineCursor = Console.CursorTop;
			int currentColumn = Console.CursorLeft;
			Console.SetCursorPosition(0, Console.CursorTop);
			Console.Write(new string(' ', Console.WindowWidth));
			Console.SetCursorPosition(currentColumn, currentLineCursor);
		}

		public static void ClearCurrentConsoleLineFromCursor()
		{
			int currentLineCursor = Console.CursorTop;
			int currentColumn = Console.CursorLeft;
			Console.Write(new string(' ', Console.WindowWidth - Console.CursorLeft));
			Console.SetCursorPosition(currentColumn, currentLineCursor);
		}

		void RefreshIterationAndStatus()
		{
			// Only do this so often
			var since = (DateTime.Now - _lastScreenUpdate);

			if (since.TotalSeconds < 1)
				return;

			_lastScreenUpdate = DateTime.Now;

			if (since.TotalMinutes > 5)
			{
				RefreshScreen();
				return;
			}

			try
			{
				Console.CursorVisible = false;

				// Display iterations

				Console.SetCursorPosition(1, 5);
				DisplayStaticText("Iteration: ");
				Console.Write(_currentIteration);
				if (_totalIterations > 0 && _totalIterations < UInt32.MaxValue)
					Console.Write(" of " + _totalIterations);

				// Display status

				Console.SetCursorPosition(4, 6);
				DisplayStaticText("Status: ");
				Console.Write(_status);
				Console.Write(new string(' ', Console.WindowWidth - Console.CursorLeft));

				// Display running

				var runSpan = (DateTime.Now - _started);

				Console.SetCursorPosition(36, 3);
				DisplayStaticText("Running: ");
				if (runSpan.Days > 0)
					Console.Write(runSpan.ToString(@"d\.hh\:mm\:ss"));
				else
					Console.Write(runSpan.ToString(@"hh\:mm\:ss"));

				// Display speed

				Console.SetCursorPosition(38, 4);
				DisplayStaticText("Speed: ");
				Console.Write((int)((_currentIteration / runSpan.TotalSeconds) * 3600));
				Console.Write("/hr");
				Console.Write(new string(' ', Console.WindowWidth - Console.CursorLeft));

				Console.SetCursorPosition(0, 9);
			}
			finally
			{
				Console.CursorVisible = true;
			}
		}

		void RefreshScreen()
		{
			try
			{
				Console.CursorVisible = false;

				Console.Clear();

				// Display title line

				Console.ForegroundColor = ConsoleColor.White;
				Console.BackgroundColor = ConsoleColor.Blue;

				Console.SetCursorPosition(0, 0);
				for (int i = 0; i < Console.WindowWidth; i++)
					Console.Write(" ");

				Console.SetCursorPosition(0, 0);
				Console.Write(_title);
				Console.SetCursorPosition(Console.WindowWidth - _copyright.Length, 0);
				Console.Write(_copyright);

				Console.ForegroundColor = ConsoleColor.Gray;
				Console.BackgroundColor = ConsoleColor.Black;

				// Display pit

				Console.SetCursorPosition(7, 2);
				DisplayStaticText("Pit: ");
				Console.Write(_context.config.pitFile);

				// Display seed

				Console.SetCursorPosition(6, 3);
				DisplayStaticText("Seed: ");
				Console.Write(_context.config.randomSeed);

				// Display started

				Console.SetCursorPosition(3, 4);
				DisplayStaticText("Started: ");
				Console.Write(_started.ToShortDateString());

				// Display running

				var runSpan = (DateTime.Now - _started);

				Console.SetCursorPosition(36, 3);
				DisplayStaticText("Running: ");
				if (runSpan.Days > 0)
					Console.Write(runSpan.ToString(@"d\.hh\:mm\:ss"));
				else
					Console.Write(runSpan.ToString(@"hh\:mm\:ss"));

				// Display speed

				Console.SetCursorPosition(38, 4);
				DisplayStaticText("Speed: ");
				Console.Write((int)((_currentIteration / (DateTime.Now - _started).TotalSeconds) * 3600));
				Console.Write("/hr     ");

				if (!string.IsNullOrEmpty(_eta))
				{
					// Display eta

					Console.SetCursorPosition(37, 5);
					DisplayStaticText("Finish: ");
					Console.Write(_eta);
				}

				// Display iterations

				Console.SetCursorPosition(1, 5);
				DisplayStaticText("Iteration: ");
				Console.Write(_currentIteration);
				if (_totalIterations > 0 && _totalIterations < UInt32.MaxValue)
					Console.Write(" of " + _totalIterations);

				// Display status

				Console.SetCursorPosition(4, 6);
				DisplayStaticText("Status: ");
				Console.Write(_status);

				// Clear rest of line
				for (int i = Console.CursorLeft; i < Console.WindowLeft; i++)
					Console.Write(' ');

				// Display faults

				Console.SetCursorPosition(0, 8);
				Console.ForegroundColor = ConsoleColor.DarkGray;
				Console.Write("---");
				Console.ForegroundColor = ConsoleColor.DarkYellow;
				Console.Write("[ ");
				Console.ForegroundColor = ConsoleColor.Red;
				Console.Write("FAULTS");
				Console.ForegroundColor = ConsoleColor.DarkRed;
				Console.Write(" - Total: " + _faults.Count);
				Console.Write(" - Major: " + _majorFaultCount.Count);
				Console.ForegroundColor = ConsoleColor.DarkYellow;
				Console.Write(" ]");
				Console.ForegroundColor = ConsoleColor.DarkGray;
				for (int i = Console.CursorLeft; i < Console.WindowWidth; i++)
					Console.Write("-");

				Console.ForegroundColor = ConsoleColor.Gray;

				int x = 9;

				for (int cnt = 0; cnt < _faults.Count && (x + cnt + 1) <= Console.WindowHeight; cnt++)
				{
					var fault = _faults[_faults.Count - (cnt + 1)];

					Console.SetCursorPosition(0, x + cnt);
					Console.Write(fault.iteration);
					Console.SetCursorPosition(10, x + cnt);
					Console.Write(fault.exploitability);
					Console.SetCursorPosition(32, x + cnt);
					Console.Write(fault.majorHash + ":" + fault.minorHash);
				}

				Console.SetCursorPosition(0, 9);
			}
			finally
			{
				Console.CursorVisible = true;
			}
		}

		public static void WriteInfoMark()
		{
			var foregroundColor = Console.ForegroundColor;
			Console.ForegroundColor = ConsoleColor.DarkGray;
			Console.Write("[");
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.Write("*");
			Console.ForegroundColor = ConsoleColor.DarkGray;
			Console.Write("] ");
			Console.ForegroundColor = foregroundColor;
		}

		public static void WriteErrorMark()
		{
			var foregroundColor = Console.ForegroundColor;
			Console.ForegroundColor = ConsoleColor.DarkGray;
			Console.Write("[");
			Console.ForegroundColor = ConsoleColor.Red;
			Console.Write("!");
			Console.ForegroundColor = ConsoleColor.DarkGray;
			Console.Write("] ");
			Console.ForegroundColor = foregroundColor;
		}
	}
}
