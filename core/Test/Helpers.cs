using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using NUnit.Framework;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Peach.Core.IO;

namespace Peach.Core.Test
{
	public static class Helpers
	{
		public static string[] ElementNames(this BitwiseStream root)
		{
			var ret = new List<string>();

			var elem = root;

			var toVisit = new List<BitwiseStream> { null };

			while (elem != null)
			{
				ret.Add(elem.Name);

				var asList = elem as BitStreamList;
				int index;

				if (asList != null)
				{
					index = toVisit.Count;
					foreach (var item in asList)
						toVisit.Insert(index, item);
				}

				index = toVisit.Count - 1;
				elem = toVisit[index];
				toVisit.RemoveAt(index);
			}

			return ret.ToArray();
		}

		public static Dictionary<T, int> Total<T>(this IEnumerable<T> seq)
		{
			var dict = new Dictionary<T, int>();

			foreach (var item in seq)
			{
				int val;
				dict.TryGetValue(item, out val);
				dict[item] = val + 1;
			}

			return dict;
		}

		public static Process StartAgent()
		{
			var startEvent = new ManualResetEvent(false);
			var process = new Process();
			var peach = Utilities.GetAppResourcePath("Peach.exe");

			if (Platform.GetOS() == Platform.OS.Windows)
			{
				process.StartInfo.FileName = peach;
				process.StartInfo.Arguments = "-a tcp";
			}
			else
			{
				process.StartInfo.FileName = "mono";
				process.StartInfo.Arguments = "--debug {0} -a tcp".Fmt(peach);
			}

			process.StartInfo.CreateNoWindow = true;
			process.StartInfo.RedirectStandardInput = true;
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.UseShellExecute = false;
			var output = new List<string>();
			process.OutputDataReceived += (sender, e) =>
			{
				if (e.Data == null)
					return;

				output.Add(e.Data);
				if (e.Data.Contains("Press ENTER to quit agent"))
					startEvent.Set();
			};

			try
			{
				process.Start();
				process.BeginOutputReadLine();

				if (!startEvent.WaitOne(5000))
				{
					Assert.Fail(string.Join("\n", output.ToArray()));
				}

				process.CancelOutputRead();

				return process;
			}
			finally
			{
				startEvent.Dispose();
				startEvent = null;
			}
		}

		public static void StopAgent(Process process)
		{
			if (!process.HasExited)
			{
				process.Kill();
				process.WaitForExit();
			}

			process.Close();
		}

		/// <summary>
		/// Get the name and IP address of the primary interface for the
		/// specified address family.
		/// If no interface can be found to satisfy the address family
		/// then null is returned.
		/// </summary>
		/// <param name="af"></param>
		/// <returns></returns>
		public static Tuple<string, IPAddress> GetPrimaryIface(AddressFamily af)
		{
			IPAddress primaryIp;

			// UDP connect to 1.1.1.1 to find the interface with the default route
			// Using NetworkInterface.GetAllInterfaces() to find the default route
			// doesn't work on all platforms. Also, OperationalStatus doesn't appear
			// to work on OSX as it always returns Unknown so the socket trick
			// is used to work around this.
			using (var s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
			{
				s.Connect(new IPAddress(0x01010101), 1);
				primaryIp = ((IPEndPoint) s.LocalEndPoint).Address;
			}

			foreach (var adapter in NetworkInterface.GetAllNetworkInterfaces())
			{
				var addrs = adapter.GetIPProperties().UnicastAddresses;

				if (addrs.Any(a => a.Address.Equals(primaryIp)))
				{
					var ip = addrs.FirstOrDefault(a => a.Address.AddressFamily == af);
					if (ip != null)
						return new Tuple<string, IPAddress>(adapter.Name, ip.Address);
				}
			}

			return null;
		}
	}
}
