using Peach.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Threading;
using Peach.Core.Runtime;
using Peach.Pro.Core.Runtime;

namespace Peach.CrashTestDummy
{
	public class Program : BaseProgram
	{
		[STAThread]
		static int Main(string[] args)
		{
			return new Program().Run(args);
		}

		class InvisibleForm : Form
		{
			readonly bool _ignore;
			const int WM_CLOSE = 0x10;

			public InvisibleForm(bool ignore)
			{
				_ignore = ignore;
				Load += InvisibleForm_Load;
			}

			void InvisibleForm_Load(object sender, EventArgs e)
			{
				Width = 1;
				Height = 1;
				Left = -200;
				Top = -200;
			}

			protected override void WndProc(ref Message m)
			{
				if (m.Msg == WM_CLOSE && _ignore)
				{
					Console.WriteLine("Ignoring WM_CLOSE");
					return;
				}

				base.WndProc(ref m);
			}
		}

		static int RunGui(List<string> args)
		{
			Console.WriteLine("Running gui");
			var ignore = args.Any(i => i == "--noclose");
			Application.Run(new InvisibleForm(ignore));
			Console.WriteLine("Exiting");
			return 0;
		}

		protected override int OnRun(List<string> args)
		{
			Console.WriteLine("Opening mutex...");
			using (var mutex = SingleInstance.CreateInstance("CrashTestDummy"))
			{
				for (int i = 0; i < 20; i++)
				{
					Console.WriteLine("Waiting for mutex...");
					if (mutex.TryLock())
					{
						Console.WriteLine("Mutex acquired");
						break;
					}
					Thread.Sleep(1000);
				}
			}
			Console.WriteLine("Mutex released");
			return 0;
		}

		protected override void AddCustomOptions(OptionSet options)
		{
			options.Add("gui", x => _cmd = RunGui);
		}
	}
}
