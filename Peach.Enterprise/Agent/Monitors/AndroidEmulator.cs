using System;
using Peach.Core;
using Peach.Core.Agent;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Net.Sockets;
using System.Net;

namespace Peach.Enterprise.Agent.Monitors
{
	[Monitor("AndroidEmulator", true)]
	[Parameter("Avd", typeof(string), "Android virtual device")]
	[Parameter("EmulatorPath", typeof(string), "Directory containing the emulator", "")]
	[Parameter("StartOnCall", typeof(string), "Start the emulator when notified by the state machine", "")]
	[Parameter("RestartEveryIteration", typeof(bool), "Restart emulator on every iteration", "false")]
	[Parameter("RestartAfterFault", typeof(bool), "Restart emulator after a fault", "true")]
	public class AndroidEmulator : Peach.Core.Agent.Monitor
	{
		static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
		static Regex reDevice = new Regex("listening on port (\\d+)");
		static string executable = Platform.GetOS() == Platform.OS.Windows ? "emulator.exe" : "emulator";

		public string Avd { get; protected set; }
		public string EmulatorPath { get; protected set; }
		public string StartOnCall { get; protected set; }
		public bool RestartEveryIteration { get; protected set; }
		public bool RestartAfterFault { get; protected set; }

		bool firstRun;
		bool fault;
		string deviceSerial;
		string emulator;
		int port;
		Exception lastError;
		Thread thread;
		AutoResetEvent readyEvt;
		AutoResetEvent errorEvt;

		public AndroidEmulator(IAgent agent, string name, Dictionary<string, Variant> args)
			: base(agent, name, args)
		{
			ParameterParser.Parse(this, args);

			emulator = Utilities.FindProgram(EmulatorPath, executable, "EmulatorPath");
		}

		public override object ProcessQueryMonitors(string query)
		{
			if (query == Name + ".DeviceSerial")
				return deviceSerial;

			return null;
		}

		public override void SessionStarting()
		{
			readyEvt = new AutoResetEvent(false);
			errorEvt = new AutoResetEvent(false);

			// Must run emulator in session starting so base class can capture the device
			firstRun = true;

			if (StartOnCall == null)
				StartEmulator();
		}

		public override void SessionFinished()
		{
			firstRun = false;

			StopEmulator();

			// Stop emulator will clean this up for us
			System.Diagnostics.Debug.Assert(thread == null);

			if (readyEvt != null)
			{
				readyEvt.Dispose();
				readyEvt = null;
			}

			if (errorEvt != null)
			{
				errorEvt.Dispose();
				errorEvt = null;
			}
		}

		public override void IterationStarting(uint iterationCount, bool isReproduction)
		{
			try
			{
				if (!firstRun && (RestartEveryIteration || StartOnCall != null || (fault && RestartAfterFault)))
				{
					StopEmulator();

					if (StartOnCall == null)
						StartEmulator();
				}
			}
			finally
			{
				firstRun = false;
				fault = false;
			}
		}

		public override bool IterationFinished()
		{
			return true;
		}

		public override void StopMonitor()
		{
			SessionFinished();
		}

		public override bool DetectedFault()
		{
			return false;
		}

		public override Fault GetMonitorData()
		{
			// A different monitor raised a fault
			fault = true;
			return null;
		}

		public override bool MustStop()
		{
			return false;
		}

		public override Variant Message(string name, Variant data)
		{
			if (this.Name == name && (string)data == "DeviceSerial")
				return new Variant(deviceSerial);

			if (name == "Action.Call" && ((string)data) == StartOnCall)
			{
				if (thread == null)
					StartEmulator();
			}

			return null;
		}

		void StartEmulator()
		{
			logger.Debug("Starting android emulator");

			System.Diagnostics.Debug.Assert(thread == null);

			// Ensure last error is cleared
			lastError = null;

			thread = new Thread(EmulatorThread);
			thread.Start();

			var handles = new[] { readyEvt, errorEvt };
			int idx = WaitHandle.WaitAny(handles);

			if (handles[idx] == errorEvt)
			{
				thread.Join();
				thread = null;

				// Last error better be set if the thread signaled an error
				System.Diagnostics.Debug.Assert(lastError != null);

				throw lastError;
			}

			logger.Debug("Android emulator '{0}' successfully started", deviceSerial);
		}

		void StopEmulator()
		{
			if (thread != null)
			{
				System.Diagnostics.Debug.Assert(port != 0);

				try
				{
					using (var cli = new TcpClient("127.0.0.1", port))
					{
						logger.Debug("Sending stop command to emulator '{0}'", deviceSerial);
						var cmd = Encoding.ASCII.GetBytes("kill\n");

						cli.Client.Send(cmd);
						cli.Client.Shutdown(SocketShutdown.Send);

						int len;
						do
						{
							try
							{
								len = cli.Client.Receive(cmd);
							}
							catch (SocketException)
							{
								len = 0;
							}
						}
						while (len > 0);

						logger.Debug("Waiting for emulator '{0}' to exit", deviceSerial);
					}
				}
				catch (Exception ex)
				{
					logger.Debug("Error when trying to gracefully stop emulator '{0}'. {1}", deviceSerial, ex.Message);
					thread.Abort();
				}

				thread.Join();
				thread = null;
				port = 0;

				logger.Debug("Emulator '{0}' to exited", deviceSerial);
			}
		}

		private int WaitForExit(Process proc, List<string> errors)
		{
			// read the lines as they come.
			// if null is returned, it's because the process finished

			Thread t1 = new Thread(new ThreadStart(delegate
			{
				try
				{
					using (var sr = proc.StandardError)
					{
						while (!sr.EndOfStream)
						{
							var line = sr.ReadLine();
							if (!string.IsNullOrEmpty(line))
							{
								if (logger.IsTraceEnabled)
									logger.Trace("{0}: {1}", Name, line);

								errors.Add(line);
							}
						}
					}
				}
				catch
				{
					// do nothing.
				}
			}));

			Thread t2 = new Thread(new ThreadStart(delegate
			{
				try
				{
					using (var sr = proc.StandardOutput)
					{
						while (!sr.EndOfStream)
						{
							string line = sr.ReadLine();
							if (!string.IsNullOrEmpty(line))
							{
								if (logger.IsTraceEnabled)
									logger.Trace("{0}: {1}", Name, line);

								var m = reDevice.Match(line);
								if (m.Success)
								{
									port = int.Parse(m.Groups[1].Value);
									deviceSerial = "emulator-" + port.ToString();
									logger.Debug("Resolved emulator instance to android device '{0}'", deviceSerial);
									readyEvt.Set();
								}
							}
						}
					}
				}
				catch
				{
					// do nothing.
				}
			}));

			t1.Start();
			t2.Start();

			// emulator.exe creates the actual emulator-<arch>.exe instance.
			// On windows, the exmulator.exe exits, so PRocess.WaitForExit()
			// will succeed and close stdout/stderr handles.  So wait for
			// the stdout/stderr readers to finish to signal emulator is finished.
			try
			{
				t1.Join();
			}
			catch (ThreadInterruptedException)
			{
			}

			try
			{
				t2.Join();
			}
			catch (ThreadInterruptedException)
			{
			}

			// get the return code from the process
			proc.WaitForExit();

			// If the exit code is non-zero, signal an error occured
			var ret = proc.ExitCode;
			if (ret != 0)
				errorEvt.Set();
			return ret;
		}

		void EmulatorThread()
		{
			var psi = new ProcessStartInfo(emulator, "-verbose -avd " + Avd);
			psi.WindowStyle = ProcessWindowStyle.Hidden;
			psi.CreateNoWindow = true;
			psi.UseShellExecute = false;
			psi.RedirectStandardError = true;
			psi.RedirectStandardOutput = true;

			var errorOutput = new List<string>();

			try
			{
				using (Process proc = Process.Start(psi))
				{
					int exitCode = WaitForExit(proc, errorOutput);
					if (exitCode != 0)
					{
						var sb = new System.Text.StringBuilder();
						sb.AppendLine("An error occurred running the android emulator.");
						foreach (var error in errorOutput)
						{
							sb.AppendLine(error);
						}
						lastError = new PeachException(sb.ToString());
					}
				}
			}
			catch (Exception ex)
			{
				lastError = new PeachException("Failed to start the android emulator.", ex);
				errorEvt.Set();
			}
		}
	}
}
