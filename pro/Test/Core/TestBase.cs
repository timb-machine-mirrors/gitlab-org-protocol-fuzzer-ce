﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using NLog;
using NLog.Config;
using NLog.Targets;
using NUnit.Framework;
using Peach.Core;
using Peach.Pro.Core;
using Peach.Pro.Core.Runtime;
using Peach.Core.Test;
using NLog.Targets.Wrappers;

namespace Peach.Pro.Test.Core
{
	[SetUpFixture]
	class TestBase : SetUpFixture
	{
		TempDirectory _tmpDir;

		public static ushort MakePort(ushort min, ushort max)
		{
			var pid = Process.GetCurrentProcess().Id;
			var seed = Environment.TickCount * pid;
			var rng = new Peach.Core.Random((uint)seed);
			var ret = (ushort)rng.Next(min, max);
			return ret;
		}

		protected override void OnSetUp()
		{
			Program.LoadPlatformAssembly();

			_tmpDir = new TempDirectory();
			Configuration.LogRoot = _tmpDir.Path;
		}

		protected override void OnTearDown()
		{
			_tmpDir.Dispose();
		}
	}

	[TestFixture]
	[Quick]
	class CommonTests : TestFixture
	{
	}
}
