﻿using System.Collections.Generic;
using NLog;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.IO;

namespace Peach.Pro.Core.Publishers
{
	[Publisher("Null", true)]
	[Parameter("MaxOutputSize", typeof(uint?), "Error if output surpasses limit.", "")]
	public class NullPublisher : Publisher
	{
		public uint? MaxOutputSize { get; private set; }

		private static NLog.Logger logger = LogManager.GetCurrentClassLogger();
		protected override NLog.Logger Logger { get { return logger; } }

		public NullPublisher(Dictionary<string, Variant> args)
			: base(args)
		{
		}

		protected override void OnOutput(BitwiseStream data)
		{
			if (MaxOutputSize.HasValue && MaxOutputSize.Value < data.Length)
				throw new PeachException("Output size '{0}' is larger than max of '{1}'.".Fmt(data.Length, MaxOutputSize));
		}

		protected override Variant OnCall(string method, List<ActionParameter> args)
		{
			return null;
		}
	}
}
