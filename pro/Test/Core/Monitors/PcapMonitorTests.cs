
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
		private string _iface;
		private Socket _socket;
		private IPEndPoint _localEp;
		private IPEndPoint _remoteEp;

		[SetUp]
		public void SetUp()
		{
			List<PcapInterface> pcaps = null;

			try
			{
				pcaps = CaptureDeviceList.Instance.OfType<LibPcapLiveDevice>().Select(d => d.Interface).ToList();
			}
			catch (Exception ex)
			{
				Assert.Ignore("Can't get pcap device list: {0}".Fmt(ex.Message));
			}

			var nics = NetworkInterface.GetAllNetworkInterfaces();

			foreach (var item in nics.Where(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType == NetworkInterfaceType.Ethernet))
			{
				var addr = item.GetIPProperties().UnicastAddresses
					.FirstOrDefault(a => a.Address.AddressFamily == AddressFamily.InterNetwork);

				if (addr == null)
					continue;

				var dev = pcaps.FirstOrDefault(p => p.MacAddress.Equals(item.GetPhysicalAddress()));
				if (dev == null)
					continue;

				var raw = addr.Address.GetAddressBytes();

				_localEp = new IPEndPoint(new IPAddress(raw), 0);

				var mask = addr.IPv4Mask.GetAddressBytes();

				Assert.AreEqual(raw.Length, mask.Length);

				for (var i = 0; i < raw.Length; ++i)
					raw[i] |= (byte) ~mask[i];

				_remoteEp = new IPEndPoint(new IPAddress(raw), 22222);

				_iface = dev.FriendlyName;

				_socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
				_socket.Bind(_localEp);

				_localEp = (IPEndPoint) _socket.LocalEndPoint;

				return;
			}

			Assert.Fail("Could not find a valid adapter to use for testing.");
		}

		[TearDown]
		public void TearDown()
		{
			if (_socket != null)
				_socket.Dispose();

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
			var runner = new MonitorRunner("Pcap", new Dictionary<string, string>
			{
				{ "Device", _iface },
			})
			{
				IterationFinished = m =>
				{
					// Capture starts in IterationStarting, and stops in IterationFinished
					for (var i = 0; i < 10; ++i)
						_socket.SendTo("Hello World", _remoteEp);

					// Ensure packets are captured
					Thread.Sleep(100);

					m.IterationFinished();
				},
				DetectedFault = m =>
				{
					Assert.False(m.DetectedFault(), "Monitor should not detect fault");

					// Trigger data collection
					return true;
				},
			};

			var faults = runner.Run();

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

			Assert.GreaterOrEqual(cnt, 10, "Captured {0} packets, expected at least 10".Fmt(cnt));
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
			var runner = new MonitorRunner("Pcap", new Dictionary<string, string>());

			var ex = Assert.Throws<PeachException>(() => runner.Run());

			Assert.AreEqual("Error, PcapMonitor requires a device name.", ex.Message);
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
			bool first = true;

			var runner = new MonitorRunner
			{
				IterationFinished = m =>
				{
					// Send test packets on IterationFinished to 1st monitor
					if (first)
					{
						// Capture starts in IterationStarting, and stops in IterationFinished
						for (var i = 0; i < count; ++i)
						{
							var ep = new IPEndPoint(_remoteEp.Address, _remoteEp.Port + i);
							_socket.SendTo("Hello World", ep);
						}

						// Ensure packets are captured
						Thread.Sleep(200);
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
			for (var i = 0; i <= count; ++i)
			{
				runner.Add("Pcap", new Dictionary<string, string>()
				{
					{ "Device", _iface },
					{ "Filter", "udp and dst port " + (_remoteEp.Port + i) },
				});
			}

			var faults = runner.Run();

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
