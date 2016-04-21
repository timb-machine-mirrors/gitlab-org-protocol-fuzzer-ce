using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Peach.Core;
using Peach.Core.Agent;

namespace Peach.Pro.Core.Agent.Monitors.Utilities
{
	public class Asan
	{
		// NOTE: Output from GCC can be slightly different than CLANG
		//       These regexes have been updated to work with both.
		private static readonly Regex AsanMatch = new Regex(@"==\d+==\s*ERROR: AddressSanitizer:");
		private static readonly Regex AsanBucket = new Regex(@"==\d+==\s*ERROR: AddressSanitizer: ([^\s]+) on.*?address ([0-9a-z]+) .*?pc ([0-9a-z]+)");
		private static readonly Regex AsanMessage = new Regex(@"(==\d+==\s*ERROR: AddressSanitizer:.*==\d+==\s*ABORTING)", RegexOptions.Singleline);
		private static readonly Regex AsanTitle = new Regex(@"==\d+==\s*ERROR: AddressSanitizer: ([^\r\n]+)");
//		private static readonly Regex AsanOom = new Regex(@"==\d+==\s*ERROR: AddressSanitizer failed to allocate (0x[^\s]+) \((.*)\) bytes of (\w+):\s(\d+)");

		/// <summary>
		/// Check string for ASAN output
		/// </summary>
		/// <param name="stderr"></param>
		/// <returns></returns>
		public static bool CheckForAsanFault(string stderr)
		{
			return AsanMatch.IsMatch(stderr);
		}

		/// <summary>
		/// Convert ASAN output into Fault
		/// </summary>
		/// <param name="stderr"></param>
		/// <returns></returns>
		public static MonitorData AsanToMonitorData(string stderr)
		{
			var data = new MonitorData();
			
			var title = AsanTitle.Match(stderr);

			// failed to allocate ASAN message is different from others
			if (title.Groups[1].Value.StartsWith("failed to allocate"))
			{
				//var oom = AsanOom.Match(stderr);

				data.Title = title.Groups[1].Value;
				data.Fault = new MonitorData.Info
				{
					Description = stderr.Substring(title.Index),
					MajorHash = Monitor2.Hash("TODO1"),
					MinorHash = Monitor2.Hash("TODO2"),
					Risk = "Out of Memory",
				};

				return data;
			}

			var bucket = AsanBucket.Match(stderr);
			var desc = AsanMessage.Match(stderr);

			data.Title = title.Groups[1].Value;
			data.Fault = new MonitorData.Info
			{
				Description = stderr.Substring(desc.Index, desc.Length),
				MajorHash = Monitor2.Hash(bucket.Groups[3].Value),
				MinorHash = Monitor2.Hash(bucket.Groups[2].Value),
				Risk = bucket.Groups[1].Value,
			};

			return data;
		}
	}
}
