﻿
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
using System.Diagnostics;
using System.IO;
using Peach.Core;
using Peach.Core.Agent;
using Monitor = Peach.Core.Agent.Monitor;

namespace Peach.Pro.OS.Windows.Agent.Monitors
{
	[Monitor("PageHeap")]
	[Description("Enables page heap debugging options for an executable")]
	[Parameter("Executable", typeof(string), "Name of executable to enable")]
	[Parameter("WinDbgPath", typeof(string), "Path to WinDbg install.  If not provided we will try and locate it.", "")]
	public class PageHeap : Monitor
	{
		public string Executable { get; set; }
		public string WinDbgPath { get; set; }

		private const string Gflags = "gflags.exe";
		private const string GflagsArgsEnable = "/p /enable \"{0}\" /full";
		private const string GflagsArgsDisable = "/p /disable \"{0}\"";

		public PageHeap(string name)
			: base(name)
		{
		}

		public override void StartMonitor(Dictionary<string, string> args)
		{
			base.StartMonitor(args);

			if (WinDbgPath == null)
				WinDbgPath = WindowsDebuggerHybrid.FindWinDbg();

			if (WinDbgPath == null)
				throw new PeachException("Error, unable to locate WinDbg, please specify using 'WinDbgPath' parameter.");
		}

		protected void Enable()
		{
			try
			{
				using (var p = new Process())
				{
					p.StartInfo = new ProcessStartInfo
					{
						FileName = Path.Combine(WinDbgPath, Gflags),
						Arguments = string.Format(GflagsArgsEnable, Executable),
						CreateNoWindow = true,
						UseShellExecute = false
					};
					p.Start();
					p.WaitForExit();
				}
			}
			catch (Exception exception)
			{
				throw new PeachException("Error, Enable PageHeap: " + exception.Message, exception);
			}
		}

		protected void Disable()
		{
			try
			{
				using (var p = new Process())
				{
					p.StartInfo = new ProcessStartInfo
					{
						FileName = Path.Combine(WinDbgPath, Gflags),
						Arguments = string.Format(GflagsArgsDisable, Executable),
						CreateNoWindow = true,
						UseShellExecute = false
					};

					p.Start();
					p.WaitForExit();
				}
			}
			catch (Exception exception)
			{
				throw new PeachException("Error, Disable PageHeap: " + exception.Message, exception);
			}
		}

		public override void SessionStarting()
		{
			Enable();
		}

		public override void SessionFinished()
		{
			Disable();
		}
	}
}
