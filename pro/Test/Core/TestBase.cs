using System;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using NUnit.Framework;
using Peach.Core;
using Peach.Pro.Core;
using Peach.Pro.Core.Runtime;
using Peach.Core.Test;
using SysProcess = System.Diagnostics.Process;

namespace Peach.Pro.Test.Core
{
	[SetUpFixture]
	internal class TestBase : SetUpFixture
	{
		TempDirectory _tmpDir;

		public static ushort MakePort(ushort min, ushort max)
		{
			var pid = SysProcess.GetCurrentProcess().Id;
			var seed = Environment.TickCount * pid;
			var rng = new Peach.Core.Random((uint)seed);

			while (true)
			{
				var ret = (ushort)rng.Next(min, max);

				using (var s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
				{
					try
					{
						s.Bind(new IPEndPoint(IPAddress.Any, ret));
					}
					catch
					{
						continue;
					}
				}

				using (var s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
				{
					try
					{
						s.Bind(new IPEndPoint(IPAddress.Any, ret));
					}
					catch
					{
						continue;
					}
				}

				return ret;
			}
		}

		[SetUp]
		public void SetUp()
		{
			DoSetUp();

			Program.LoadPlatformAssembly();

			_tmpDir = new TempDirectory();
			Configuration.LogRoot = _tmpDir.Path;
		}

		[TearDown]
		public void TearDown()
		{
			_tmpDir.Dispose();

			DoTearDown();
		}
	}

	[TestFixture]
	[Quick]
	internal class CommonTests : TestFixture
	{
		public CommonTests()
			: base(Assembly.GetExecutingAssembly())
		{
		}

		[Test]
		public void AssertWorks()
		{
			DoAssertWorks();
		}

		[Test]
		public void NoMissingAttributes()
		{
			DoNoMissingAttributes();
		}
	}
}
