using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using NLog;
using Peach.Core.IO;

namespace Peach.Core.Publishers
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

		protected override Variant OnCall(string method, List<Dom.ActionParameter> args)
		{
			return null;
		}
	}
}
