using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Demon
{
	class Program
	{
		static void Main(string[] args)
		{
			new Program().Run(args);
		}

		Program()
		{
		}

		void Run(string[] args)
		{
			Console.WriteLine("Launching server...");
			StartServer();

			Thread.Sleep(1000);

			Process.Start("http://localhost:5000");

			var watcher = new FileSystemWatcher(args[0])
			{
				IncludeSubdirectories = true,
				NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.DirectoryName | NotifyFilters.FileName
			};
			watcher.Created += OnCreated;
			watcher.Changed += OnChanged;
			watcher.Deleted += OnDeleted;
			watcher.Renamed += OnRenamed;
			watcher.EnableRaisingEvents = true;

			Console.WriteLine("Waiting for changes...");
			Console.WriteLine("Press any key to quit.");

			Console.Read();

			KillServer();
		}

		private void OnCreated(object sender, FileSystemEventArgs args)
		{
			Console.WriteLine("OnCreated: {0}", args.FullPath);
			ProcessEvent(args.FullPath);
		}

		private void OnChanged(object sender, FileSystemEventArgs args)
		{
			Console.WriteLine("OnChanged: {0}", args.FullPath);
			ProcessEvent(args.FullPath);
		}

		private void OnRenamed(object sender, RenamedEventArgs args)
		{
			Console.WriteLine("OnRenamed: {0}", args.FullPath);
			ProcessEvent(args.FullPath);
		}

		private void OnDeleted(object sender, FileSystemEventArgs args)
		{
			Console.WriteLine("OnDeleted: {0}", args.FullPath);
			ProcessEvent(args.FullPath);
		}

		private int _last = 0;
		private Process _proc;

		private void ProcessEvent(string path)
		{
			var current = Interlocked.Increment(ref _last);
			Task.Delay(500).ContinueWith(task =>
			{
				if (current == _last)
					RestartServer();
				task.Dispose();
			});
		}

		private void RestartServer()
		{
			Console.WriteLine("RestartServer");

			if (_proc != null)
				KillServer();
			StartServer();
		}

		private void StartServer()
		{
			var srcdir = @"C:\Home\src\peach-pro\peach-web\.depproj\PeachWeb\bin\win_debug_x64";
			var cwd = @"C:\Home\src\peach-pro\peach-web\web\PeachWeb";

			var tgtdir = CopyAssemblies(srcdir);

			_proc = new Process
			{
				StartInfo = new ProcessStartInfo
				{
					FileName = Path.Combine(tgtdir, "Peach.Web.exe"),
					Arguments = "--environment Development",
					WorkingDirectory = cwd,
					UseShellExecute = false,
				},
			};
			_proc.Start();
		}

		private void KillServer()
		{
			_proc.Kill();
			_proc.WaitForExit(30000);
		}

		private string CopyAssemblies(string srcdir)
		{
			var tgtdir = Path.Combine(HomePath, ".demon", "Peach.Web");
			Directory.CreateDirectory(tgtdir);

			foreach (var source in Directory.EnumerateFiles(srcdir))
			{
				var fi = new FileInfo(source);
				var target = Path.Combine(tgtdir, fi.Name);
				File.Copy(source, target, true);
			}

			return tgtdir;
		}

		private string HomePath => Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%");
	}
}
