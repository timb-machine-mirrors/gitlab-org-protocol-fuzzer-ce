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
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Peach.Core;
using Peach.Core.Agent;
using Encoding = Peach.Core.Encoding;
using Monitor = Peach.Core.Agent.Monitor;

namespace Peach.Pro.OS.Windows.Agent.Monitors
{
	[Monitor("PopupWatcher")]
	[Description("Closes windows based on title")]
	[Parameter("WindowNames", typeof(string[]), "Window names separated by a ','")]
	[Parameter("Fault", typeof(bool), "Trigger fault when a window is found", "false")]
	public class PopupWatcher : Monitor
	{
		public string[] WindowNames { get; set; }
		public bool Fault { get; set; }

		#region P/Invokes

		delegate bool EnumDelegate(IntPtr hWnd, IntPtr lParam);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		static extern int GetWindowTextLength(IntPtr hWnd);
		[DllImport("user32", CharSet = CharSet.Auto, SetLastError = true)]
		static extern int GetWindowText(IntPtr hWnd, [Out, MarshalAs(UnmanagedType.LPTStr)] StringBuilder lpString, int nLen);
		[DllImport("user32.dll", SetLastError = true)]
		static extern bool EnumWindows(EnumDelegate lpEnumFunc, IntPtr lParam);
		[DllImport("user32.dll", SetLastError = true)]
		static extern bool EnumChildWindows(IntPtr hWndParent, EnumDelegate lpEnumFunc, IntPtr lParam);
		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		static extern int PostMessage(IntPtr hWnd, UInt32 msg, IntPtr wParam, IntPtr lParam);
		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

		// ReSharper disable once InconsistentNaming
		private const uint WM_CLOSE = 0x0010;

		#endregion

		readonly SortedSet<string> _closedWindows = new SortedSet<string>();
		readonly object _lock = new object();

		Thread _worker;
		ManualResetEvent _event;
		bool _continue;
		long _workerCount;
		MonitorData _data;

		public PopupWatcher(string name)
			: base(name)
		{
		}

		bool EnumHandler(IntPtr hWnd, IntPtr lParam)
		{
			var nLength = GetWindowTextLength(hWnd);
			if (nLength == 0)
				return _continue;

			var strbTitle = new StringBuilder(nLength + 1);
			nLength = GetWindowText(hWnd, strbTitle, strbTitle.Capacity);
			if (nLength == 0)
				return _continue;

			var strTitle = strbTitle.ToString();

			Debug.Assert(WindowNames != null);
			Debug.Assert(WindowNames.Length > 0);

			if (WindowNames.Any(n => strTitle.IndexOf(n, StringComparison.Ordinal) > -1))
			{
				PostMessage(hWnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
				SendMessage(hWnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);

				lock (_lock)
				{
					_closedWindows.Add(strTitle);
					OnInternalEvent(EventArgs.Empty);
				}

				_continue &= WindowNames.Length > 1;
				return _continue;
			}

			// Recursively check child windows
			EnumChildWindows(hWnd, EnumHandler, IntPtr.Zero);

			return _continue;
		}


		public void Work()
		{
			while (!_event.WaitOne(200))
			{
				// Reset continue for subsequent enum call
				_continue = true;

				// Find top level windows
				EnumWindows(EnumHandler, IntPtr.Zero);

				// Increment counter
				Interlocked.Increment(ref _workerCount);
			}
		}

		public override void SessionStarting()
		{
			if (WindowNames == null || WindowNames.Length == 0)
				return;

			_event = new ManualResetEvent(false);

			_worker = new Thread(Work);
			_worker.Start();
		}

		public override void SessionFinished()
		{
			if (_worker != null)
			{
				_event.Set();

				_worker.Join();
				_worker = null;

				_event.Close();
				_event = null;
			}
		}

		public override bool DetectedFault()
		{
			return _data != null && _data.Fault != null;
		}


		public override void IterationStarting(IterationStartingArgs args)
		{
			Interlocked.Exchange(ref _workerCount, 0);
		}

		public override void IterationFinished()
		{
			_data = null;

			// Wait for the window closer thread to fire once more
			var start = Interlocked.Read(ref _workerCount);
			var cnt = 0;

			do
			{
				Thread.Sleep(100);
			}
			while (start == Interlocked.Read(ref _workerCount) && cnt++ < 10);

			lock (_lock)
			{
				if (_closedWindows.Count > 0)
				{
					_data = new MonitorData
					{
						Title = "Closed {0} popup window{1}.".Fmt(_closedWindows.Count, _closedWindows.Count > 1 ? "s" : ""),
						Data = new Dictionary<string, byte[]>()
					};

					var eol = Environment.NewLine;
					var desc = "Window Titles:{0}{1}".Fmt(eol, string.Join(eol, _closedWindows));

					if (Fault)
						_data.Fault = new MonitorData.Info { Description = desc };
					else
						_data.Data.Add("ClosedWindows.txt", Encoding.UTF8.GetBytes(desc));

					_closedWindows.Clear();
				}
			}
		}

		public override MonitorData GetMonitorData()
		{
			return _data;
		}
	}
}

