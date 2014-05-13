using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Reflection;

using Peach.Core;
using Peach.Core.Agent;

using NLog;

namespace Peach.Enterprise.Runtime
{
	public class ConsoleWatcher : Watcher
	{
		string _title = "Peach Pro v";
		string _copyright = "Copyright (c) Deja vu Security";

		Stopwatch timer = new Stopwatch();
		uint startIteration = 0;
		bool reproducing = false;

		RunContext _context = null;
		uint _currentIteration = 0;
		uint _totalIterations = 0;
		List<Fault> _faults = new List<Fault>();
		Dictionary<string, int> _majorFaultCount = new Dictionary<string, int>();
		DateTime _started = DateTime.Now;
		string _status = "";
		string _eta = "";

		public ConsoleWatcher() : base()
		{
			_title += Assembly.GetExecutingAssembly().GetName().Version;
		}

		protected override void Engine_ReproFault(RunContext context, uint currentIteration, Peach.Core.Dom.StateModel stateModel, Fault [] faultData)
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

			foreach(Fault fault in faultData)
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
			if(totalIterations != null)
				_totalIterations = totalIterations.Value;

			string strEta = "-";

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

				strEta = remain.ToString("g");
				_eta = strEta;
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
			_status = "Test '" + context.test.name + "' error: " + e.Message;
			RefreshScreen();
		}

		protected override void Engine_TestFinished(RunContext context)
		{
			_status = "Test '" + context.test.name + "' finished.";
			RefreshScreen();
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

		void DisplayStatusText(string text)
		{
			Console.Write(text);
		}

		void DisplayText(string text, ConsoleColor color)
		{
			Console.ForegroundColor = color;
			Console.Write(text);
			Console.ForegroundColor = ConsoleColor.Gray;
		}

		void RefreshIterationAndStatus()
		{
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

			if (_currentIteration % 4 == 0)
			{
				// Display running

				Console.SetCursorPosition(36, 3);
				DisplayStaticText("Running: ");
				Console.Write((DateTime.Now - _started).ToString(@"hh\:mm\:ss"));

				// Display speed

				Console.SetCursorPosition(38, 4);
				DisplayStaticText("Speed: ");
				Console.Write((int)((_currentIteration / (DateTime.Now - _started).TotalSeconds) * 3600));
				Console.Write("/hr         ");
			}
		}

		void RefreshScreen()
		{
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

			Console.SetCursorPosition(36, 3);
			DisplayStaticText("Running: ");
			Console.Write((DateTime.Now - _started).ToString(@"hh\:mm\:ss"));

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
			if(_totalIterations > 0 && _totalIterations < UInt32.MaxValue)
				Console.Write(" of " + _totalIterations);

			// Display status

			Console.SetCursorPosition(4, 6);
			DisplayStaticText("Status: ");
			Console.Write(_status);
			for (int i = _status.Length; i < Console.WindowWidth; i++)
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

			for (int cnt = 0; cnt < _faults.Count && (x+cnt+1) <= Console.WindowHeight; cnt++)
			{
				var fault = _faults[_faults.Count - (cnt+1)];

				Console.SetCursorPosition(0, x + cnt);
				Console.Write(fault.iteration);
				Console.SetCursorPosition(10, x + cnt);
				Console.Write(fault.exploitability);
				Console.SetCursorPosition(32, x + cnt);
				Console.Write(fault.majorHash + ":" + fault.minorHash);
			}
		}
	}
}

// end
