﻿using System;
using System.Collections.Generic;
using System.IO;

namespace Peach.Core.Publishers
{
	[Publisher("ConsoleHex", true)]
	[Publisher("StdoutHex")]
	[Publisher("stdout.StdoutHex")]
	[Parameter("BytesPerLine", typeof(int), "How many bytes per row of text", "16")]
	public class ConsoleHexPublisher : ConsolePublisher
	{
		public int BytesPerLine { get; set; }

		public ConsoleHexPublisher(Dictionary<string, Variant> args)
			: base(args)
		{
		}

		protected override void OnOutput(System.IO.Stream data)
		{
			Utilities.HexDump(data, stream, BytesPerLine);
		}
	}
}
