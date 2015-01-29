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
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Test;
using SharpPcap;
using SharpPcap.LibPcap;

namespace Peach.Pro.Test.Core.Monitors
{
	[TestFixture]
	[Category("Peach")]
	class PcapMonitorTests
	{
		private AutoResetEvent _evt;
		private string _iface;
		private Socket _socket;
		private IPEndPoint _localEp;
		private IPEndPoint _remoteEp;

		[SetUp]
		public void SetUp()
		{
			_localEp = new IPEndPoint(IPAddress.None, 0);
			_remoteEp = new IPEndPoint(IPAddress.Parse("1.1.1.1"), 22222);

			using (var s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
			{
				try
				{
					s.Connect(_remoteEp);
					_localEp.Address = ((IPEndPoint)s.LocalEndPoint).Address;
				}
				catch (SocketException)
				{
					Assert.Ignore("Couldn't find primary local IP address.");
				}
			}

			var macAddr = NetworkInterface.GetAllNetworkInterfaces()
				.Where(n => n.GetIPProperties().UnicastAddresses.Any(a => a.Address.Equals(_localEp.Address)))
				.Select(n => n.GetPhysicalAddress())
				.First();

			_iface = CaptureDeviceList.Instance
				.OfType<LibPcapLiveDevice>()
				.Select(p => p.Interface)
				.Where(i => i.MacAddress.Equals(macAddr))
				.Select(i => i.FriendlyName)
				.FirstOrDefault();

			if (_iface == null)
				Assert.Ignore("Could not find a valid adapter to use for testing.");

			_socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			_socket.Bind(_localEp);
			_socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, true);

			_localEp = (IPEndPoint) _socket.LocalEndPoint;

			_evt = new AutoResetEvent(false);
		}

		[TearDown]
		public void TearDown()
		{
			if (_evt != null)
				_evt.Dispose();

			if (_socket != null)
				_socket.Dispose();

			_evt = null;
			_socket = null;
			_iface = null;
			_localEp = null;
			_remoteEp = null;
		}

		[Test]
		public void BasicTest()
		{
			var runner = new MonitorRunner("Pcap", new Dictionary<string, string>
			{
				{ "Device", _iface },
			});

			var faults = runner.Run();

			Assert.AreEqual(0, faults.Length);
		}

		[Test]
		public void DataCollection()
		{
			const int max = 10;
			var num = 0;

			var runner = new MonitorRunner("Pcap", new Dictionary<string, string>
			{
				{ "Device", _iface },
			})
			{
				SessionStarting = m =>
				{
					m.InternalEvent += (o, e) =>
					{
						if (++num == max)
							_evt.Set();
					};

					m.SessionStarting();
				},
				IterationFinished = m =>
				{
					// Capture starts in IterationStarting, and stops in IterationFinished
					for (var i = 0; i < max; ++i)
						_socket.SendTo("Hello World", _remoteEp);

					// Ensure packets are captured
					if (!_evt.WaitOne(5000))
						Assert.Fail("Didn't receive packets within 5 second.");

					m.IterationFinished();
				},
				DetectedFault = m =>
				{
					Assert.False(m.DetectedFault(), "Monitor should not detect fault");

					// Trigger data collection
					return true;
				},
			};

			Fault[] faults;

			using (var si = SingleInstance.CreateInstance("Peach.Pro.Test.Core.Monitors.PcapMonitorTests.DataCollection"))
			{
				si.Lock();

				faults = runner.Run();
			}

			Assert.AreEqual(1, faults.Length);
			Assert.AreEqual(FaultType.Data, faults[0].type);
			Assert.AreEqual("PcapMonitor", faults[0].detectionSource);
			Assert.AreEqual(1, faults[0].collectedData.Count);
			Assert.AreEqual("NetworkCapture.pcap", faults[0].collectedData[0].Key);

			const string begin = "Collected ";
			Assert.That(faults[0].description, Is.StringStarting(begin));

			const string end = " packets.";
			Assert.That(faults[0].description, Is.StringEnding(end));

			var str = faults[0].description.Substring(begin.Length, faults[0].description.Length - begin.Length - end.Length);
			var cnt = int.Parse(str);

			Assert.GreaterOrEqual(cnt, max, "Captured {0} packets, expected at least 10".Fmt(cnt));
		}

		[Test]
		public void MultipleIterationsTest()
		{
			var runner = new MonitorRunner("Pcap", new Dictionary<string, string>
			{
				{ "Device", _iface },
			});

			var faults = runner.Run(10);

			Assert.AreEqual(0, faults.Length);
		}

		[Test]
		public void BadDeviceTest()
		{
			var runner = new MonitorRunner("Pcap", new Dictionary<string, string>
			{
				{ "Device", "Some Unknown Device" },
			});

			var ex = Assert.Throws<PeachException>(() => runner.Run());

			Assert.AreEqual("Error, PcapMonitor was unable to locate device 'Some Unknown Device'.", ex.Message);
		}

		[Test]
		public void NoDeviceTest()
		{
			var ex = Assert.Throws<PeachException>(() =>
				new MonitorRunner("Pcap", new Dictionary<string, string>()));

			Assert.AreEqual("Could not start monitor \"Pcap\".  Monitor 'Pcap' is missing required parameter 'Device'.", ex.Message);
		}

		[Test]
		public void BadFilterTest()
		{
			var runner = new MonitorRunner("Pcap", new Dictionary<string, string>
			{
				{ "Device", _iface },
				{ "Filter", "Bad filter string" },
			});

			var ex = Assert.Throws<PeachException>(() => runner.Run());

			Assert.AreEqual("Error, PcapMonitor was unable to set the filter 'Bad filter string'.", ex.Message);
		}

		[Test]
		public void MultipleMonitorsTest()
		{
			const int count = 5;
			var num = 0;
			bool first = true;

			var runner = new MonitorRunner
			{
				SessionStarting = m =>
				{
					m.InternalEvent += (s, e) =>
					{
						if (++num == count)
							_evt.Set();
					};

					m.SessionStarting();
				},
				IterationFinished = m =>
				{
					// Send test packets on IterationFinished to 1st monitor
					if (first)
					{
						// Capture starts in IterationStarting, and stops in IterationFinished
						for (var i = 0; i < count; ++i)
						{
							var ep = new IPEndPoint(_remoteEp.Address, _remoteEp.Port + 1 + i);
							_socket.SendTo("Hello World", ep);
						}

						// Ensure packets are captured
						if (!_evt.WaitOne(5000))
							Assert.Fail("Didn't receive packets within 5 second.");
					}

					first = false;

					m.IterationFinished();
				},
				DetectedFault = m =>
				{
					Assert.False(m.DetectedFault(), "Monitor should not detect fault");

					// Trigger data collection
					return true;
				},
			};

			// Add one extra that is not sent to
			// Skew by one so we don't conflict with DataCollection test port
			for (var i = 0; i <= count; ++i)
			{
				runner.Add("Pcap", new Dictionary<string, string>()
				{
					{ "Device", _iface },
					{ "Filter", "udp and dst port " + (_remoteEp.Port + 1 + i) },
				});
			}

			Fault[] faults;

			using (var si = SingleInstance.CreateInstance("Peach.Pro.Test.Core.Monitors.PcapMonitorTests.MultipleMonitorsTest"))
			{
				si.Lock();

				faults = runner.Run();
			}

			// Expect a fault for each pcap monitor
			Assert.AreEqual(count + 1, faults.Length);

			for (var i = 0; i <= count; ++i)
			{
				var f = faults[i];

				Assert.AreEqual(FaultType.Data, f.type);
				Assert.AreEqual("PcapMonitor", f.detectionSource);
				Assert.AreEqual(1, f.collectedData.Count);
				Assert.AreEqual("NetworkCapture.pcap", f.collectedData[0].Key);

				var msg = "Collected {0} packets.".Fmt(i == count ? 0 : 1);

				Assert.AreEqual(msg, f.description);
			}


			Assert.AreEqual(FaultType.Data, faults[0].type);
			Assert.AreEqual("PcapMonitor", faults[0].detectionSource);
			Assert.AreEqual(1, faults[0].collectedData.Count);
			Assert.AreEqual("NetworkCapture.pcap", faults[0].collectedData[0].Key);
		}
	}
}
