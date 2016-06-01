using Peach.Core;
using System;
using System.Linq;
using System.Windows.Forms;
using System.Threading;
using Peach.Pro.Core.Runtime;

namespace Peach.CrashTestDummy
{
	public class Program
	{
		[STAThread]
		static int Main(string[] args)
		{
			if (args.Any(i => i == "--gui"))
				return RunGui(args);

			// Peach.Core.dll
			ClassLoader.LoadAssembly(typeof(ClassLoader).Assembly);

			// Peach.Pro.dll
			ClassLoader.LoadAssembly(typeof(BaseProgram).Assembly);

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

		static int RunGui(string[] args)
		{
			Console.WriteLine("Running gui");
			var ignore = args.Any(i => i == "--noclose");
			Application.Run(new InvisibleForm(ignore));
			Console.WriteLine("Exiting");
			return 0;
		}
	}
}
