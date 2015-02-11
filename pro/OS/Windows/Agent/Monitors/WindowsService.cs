//
// Copyright (c) Michael Eddington
//
// Permission is hereby granted, free of charge, to any person obtaining a copy 
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights 
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
// copies of the Software, and to permit persons to whom the Software is 
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in	
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//

// Authors:
//   Michael Eddington (mike@dejavusecurity.com)

// $Id$

using System;
using System.Collections.Generic;
using System.ServiceProcess;
using NLog;
using Peach.Core;
using Peach.Core.Agent;

namespace Peach.Pro.OS.Windows.Agent.Monitors
{
	[Monitor("WindowsService")]
	[Description("Controls a Windows service")]
	[Parameter("Service", typeof(string), "The name that identifies the service to the system. This can also be the display name for the service.")]
	[Parameter("MachineName", typeof(string), "The computer on which the service resides. (optional, defaults to local machine)", "")]
	[Parameter("FaultOnEarlyExit", typeof(bool), "Fault if service exists early. (defaults to false)", "false")]
	[Parameter("Restart", typeof(bool), "Restart service on every iteration. (defaults to false)", "false")]
	[Parameter("StartTimeout", typeof(int), "Time in minutes to wait for service start. (defaults to 1 minute)", "1")]
	public class WindowsService : Monitor
	{
		static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();

		public string Service { get; set; }
		public string MachineName { get; set; }
		public bool FaultOnEarlyExit { get; set; }
		public bool Restart { get; set; }
		public int StartTimeout { get; set; }

		ServiceController _sc;
		MonitorData _data;

		public WindowsService(string name)
			: base(name)
		{
		}

		public override void StartMonitor(Dictionary<string, string> args)
		{
			string val;
			if (args.TryGetValue("StringTimout", out val) && !args.ContainsKey("StringTimeout"))
			{
				Logger.Info("The parameter 'StringTimout' on the monitor 'WindowsService' is deprecated.  Use the parameter 'StringTimeout' instead.");
				args["StringTimeout"] = val;
				args.Remove("StringTimout");
			}

			base.StartMonitor(args);

			if (string.IsNullOrEmpty(MachineName))
			{
				_sc = new ServiceController(Service);
				if (_sc == null)
					throw new PeachException("WindowsService monitor was unable to connect to service '{0}'.".Fmt(Service));
			}
			else
			{
				_sc = new ServiceController(Service, MachineName);
				if (_sc == null)
					throw new PeachException("WindowsService monitor was unable to connect to service '{0}' on computer '{1}'.".Fmt(Service, MachineName));
			}
		}

		private void ControlService(string what, Action action)
		{
			if (MachineName == null)
				Logger.Debug("Attempting to {0} service {0}", what, Service);
			else
				Logger.Debug("Attempting to {0} service {1} on machine {2}", what, Service, MachineName);

			try
			{
				_sc.Refresh();

				action();

			}
			catch (System.ServiceProcess.TimeoutException ex)
			{
				var pe = new PeachException(
					"WindowsService monitor was unable to {0} service '{1}'{2}.".Fmt(
						what,
						Service,
						MachineName == null ? "" : "on machine '{0}'".Fmt(MachineName)),
					ex);

				Logger.Debug(pe.Message);
				throw pe;
			}
		}

		private void StartService()
		{
			ControlService("start", () =>
			{
				switch (_sc.Status)
				{
					case ServiceControllerStatus.ContinuePending:
						break;
					case ServiceControllerStatus.Paused:
						_sc.Continue();
						break;
					case ServiceControllerStatus.PausePending:
						_sc.WaitForStatus(ServiceControllerStatus.Paused, TimeSpan.FromMilliseconds(StartTimeout));
						_sc.Continue();
						break;
					case ServiceControllerStatus.Running:
						break;
					case ServiceControllerStatus.StartPending:
						break;
					case ServiceControllerStatus.Stopped:
						_sc.Start();
						break;
					case ServiceControllerStatus.StopPending:
						_sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromMilliseconds(StartTimeout));
						_sc.Start();
						break;
				}

				_sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromMilliseconds(StartTimeout));
			});
		}

		protected void StopService()
		{
			ControlService("stop", () =>
			{
				switch (_sc.Status)
				{
					case ServiceControllerStatus.ContinuePending:
						_sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromMilliseconds(StartTimeout));
						_sc.Stop();
						break;
					case ServiceControllerStatus.Paused:
						_sc.Stop();
						break;
					case ServiceControllerStatus.PausePending:
						_sc.WaitForStatus(ServiceControllerStatus.Paused, TimeSpan.FromMilliseconds(StartTimeout));
						_sc.Stop();
						break;
					case ServiceControllerStatus.Running:
						_sc.Stop();
						break;
					case ServiceControllerStatus.StartPending:
						_sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromMilliseconds(StartTimeout));
						_sc.Stop();
						break;
					case ServiceControllerStatus.Stopped:
						break;
					case ServiceControllerStatus.StopPending:
						break;
				}

				_sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromMilliseconds(StartTimeout));
			});
		}

		public override void StopMonitor()
		{
			if (_sc != null)
			{
				_sc.Close();
				_sc = null;
			}
		}

		public override void IterationStarting(IterationStartingArgs args)
		{
			_data = null;

			if (Restart)
				StopService();

			StartService();
		}

		public override bool DetectedFault()
		{
			if (_data == null)
			{
				_sc.Refresh();

				if (FaultOnEarlyExit && _sc.Status != ServiceControllerStatus.Running)
				{
					Logger.Info("DetectedFault() - Fault detected, process exited early");

					_data = new MonitorData
					{
						Title = MachineName == null
							? "The windows service '{0}' stopped early.".Fmt(Service)
							: "The windows service '{0}' on machine '{1}' stopped early.".Fmt(Service, MachineName),
						Data = new Dictionary<string, byte[]>(),
						Fault = new MonitorData.Info()
					};
				}
			}

			return _data != null;
		}

		public override MonitorData GetMonitorData()
		{
			// Wil be called if a different monitor records a fault
			// so don't assume the service has stopped early.
			return _data;
		}
	}
}
