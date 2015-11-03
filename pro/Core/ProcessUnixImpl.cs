using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using Peach.Core;
using Logger = NLog.Logger;
using SysProcess = System.Diagnostics.Process;

namespace Peach.Pro.Core
{
	public abstract class ProcessUnixImpl : Process
	{
		const int SIGKILL = 9;
		const int SIGTERM = 15;

		protected ProcessUnixImpl(Logger logger) : base(logger)
		{
		}

		protected override SysProcess CreateProcess(
			string executable,
			string arguments,
			string workingDirectory,
			Dictionary<string, string> environment)
		{
			var listener = new TcpListener(IPAddress.Loopback, 0);
			listener.Start();
			var local = (IPEndPoint)listener.LocalEndpoint;

			_logger.Debug("CreateProcess(): TcpListener bound to: {0}", local);

			var args = string.Join(" ",
				"--debugger-agent=transport=dt_socket,address=127.0.0.1:{0},setpgid=y,suspend=n".Fmt(local.Port),
				Utilities.GetAppResourcePath("PeachTrampoline.exe"),
				executable,
				arguments
			);

			var si = new System.Diagnostics.ProcessStartInfo
			{
				FileName = "mono",
				Arguments = args,
				UseShellExecute = false,
				RedirectStandardInput = true,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				WorkingDirectory = workingDirectory ?? "",
			};

			if (environment != null)
				environment.ForEach(x => si.EnvironmentVariables[x.Key] = x.Value);

			_logger.Debug("CreateProcess(): \"{0} {1}\"", executable, arguments);
			var process = SysProcess.Start(si);

			using (var tcp = listener.AcceptTcpClient())
			using (var stream = tcp.GetStream())
			{
				var handshake = new byte[13]; // "DWP handshake"
				var header = new byte[256];

				_logger.Debug("CreateProcess(): Reading handshake...");
				stream.Read(handshake, 0, handshake.Length);
				_logger.Debug("CreateProcess(): Echo: {0}", Encoding.UTF8.GetString(handshake));
				stream.Write(handshake, 0, handshake.Length);

				// wait until EOF
				while (true)
				{
					var ret = stream.Read(header, 0, header.Length);
					_logger.Debug("CreateProcess(): Read {0} bytes", ret);
					if (ret == 0)
						break;
				}
			}

			return process;
		}

		protected override void Terminate(SysProcess process)
		{
			var ret = killpg(process.Id, SIGTERM);
			if (ret == -1)
				throw new Win32Exception("killpg({0}, SIGTERM) failed".Fmt(process.Id)); // reads errno internally
		}

		protected override void Kill(SysProcess process)
		{
			var ret = killpg(process.Id, SIGKILL);
			if (ret == -1)
				throw new Win32Exception("killpg({0}, SIGKILL) failed".Fmt(process.Id)); // reads errno internally
		}

		protected override void WaitForProcessGroup(SysProcess process)
		{
			while (getpgid(process.Id) != process.Id)
				Thread.Sleep(10);
		}

		[DllImport("libc", SetLastError = true)]
		private static extern int killpg(int pgrp, int sig);

		[DllImport("libc", SetLastError = true)]
		private static extern int getpgid(int pid);
	}
}
