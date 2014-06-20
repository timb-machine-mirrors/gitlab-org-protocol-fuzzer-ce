using Peach.Core;

using System;
using System.IO;
using System.Reflection;

namespace Peach.Enterprise.Runtime
{
	public class Program : Peach.Core.Runtime.Program
	{
		public Program(string[] args)
			: base(args)
		{
		}

		/// <summary>
		/// Copyright message
		/// </summary>
		public override string Copyright
		{
			get { return "Copyright (c) Deja vu Security"; }
		}

		/// <summary>
		/// Product name
		/// </summary>
		public override string ProductName
		{
			get { return "Peach Pro v" + Assembly.GetExecutingAssembly().GetName().Version; }
		}

		protected override Watcher GetUIWatcher()
		{
			try
			{
				if (config.debug > 0)
				{
					return new Peach.Core.Runtime.ConsoleWatcher();
				}

				// Ensure console is interactive
				Console.Clear();

				return new Peach.Enterprise.Runtime.ConsoleWatcher();

			}
			catch (IOException)
			{
				return new Peach.Core.Runtime.ConsoleWatcher();
			}
		}

		protected override Analyzer GetParser()
		{
			var parser = new Godel.Core.GodelPitParser();
			Analyzer.defaultParser = parser;

			return base.GetParser();
		}

		protected override void RunEngine()
		{
			using (var svc = new WebServices.WebService())
			{
				svc.Start(new string[0]);

				// Add the web logger as the 1st logger to each test
				foreach (var test in dom.tests)
					test.loggers.Insert(0, svc.Logger);

				base.RunEngine();
			}
		}
	}
}
