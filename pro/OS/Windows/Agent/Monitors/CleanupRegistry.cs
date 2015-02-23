
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

using System.Collections.Generic;
using Microsoft.Win32;
using NLog;
using Peach.Core;
using Peach.Core.Agent;
using Monitor = Peach.Core.Agent.Monitor2;
using DescriptionAttribute = System.ComponentModel.DescriptionAttribute;

namespace Peach.Pro.OS.Windows.Agent.Monitors
{
	[Monitor("CleanupRegistry")]
	[Description("Remove a registry key or a key's children")]
	[Parameter("Key", typeof(string), "Registry key to remove.")]
	[Parameter("ChildrenOnly", typeof(bool), "Only cleanup sub-keys. (defaults to false)", "false")]
	public class CleanupRegistry : Monitor
	{
		static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();

		public string Key { get; set; }
		public bool ChildrenOnly { get; set; }
		public RegistryKey Root { get; set; }

		public CleanupRegistry(string name)
			: base(name)
		{
		}

		public override void StartMonitor(Dictionary<string, string> args)
		{
			base.StartMonitor(args);

			if (Key.StartsWith("HKCU\\"))
				Root = Registry.CurrentUser;
			else if (Key.StartsWith("HKCC\\"))
				Root = Registry.CurrentConfig;
			else if (Key.StartsWith("HKLM\\"))
				Root = Registry.LocalMachine;
			else if (Key.StartsWith("HKPD\\"))
				Root = Registry.PerformanceData;
			else if (Key.StartsWith("HKU\\"))
				Root = Registry.Users;
			else
				throw new PeachException("Error, CleanupRegistry monitor Key parameter must be prefixed with HKCU, HKCC, HKLM, HKPD, or HKU.");

			Key = Key.Substring(Key.IndexOf("\\", System.StringComparison.Ordinal) + 1);
		}

		public override void IterationStarting(IterationStartingArgs args)
		{
			if (!ChildrenOnly)
			{
				Logger.Debug("Removing key: " + Key);
				Root.DeleteSubKeyTree(Key, false);
				return;
			}

			var key = Root.OpenSubKey(Key, true);
			if (key == null)
				return;

			foreach (var subkey in key.GetSubKeyNames())
			{
				Logger.Debug("Removing subkey: " + subkey);
				key.DeleteSubKeyTree(subkey, false);
			}
		}
	}
}
