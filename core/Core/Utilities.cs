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
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using NLog;
using NLog.Targets;
using NLog.Config;
using System.Threading;

namespace Peach.Core
{
	/// <summary>
	/// Helper class to add a debug listener so asserts get written to the console.
	/// </summary>
	// NOTE: Tell msvs this is not a 'Component'
	[DesignerCategory("Code")]
	public class AssertWriter : TraceListener
	{
		static readonly NLog.Logger Logger = LogManager.GetLogger("TraceListener");

		public static void Register()
		{
			Register<AssertWriter>();
		}

		public static void Register<T>() where T : AssertWriter, new()
		{
			Debug.Listeners.Insert(0, new T());
		}

		protected virtual void OnAssert(string message)
		{
			Console.WriteLine(message);
		}

		public override void Fail(string message)
		{
			var sb = new StringBuilder();

			sb.AppendLine("Assertion " + message);
			sb.AppendLine(new StackTrace(2, true).ToString());

			OnAssert(sb.ToString());
		}

		public override void Write(string message)
		{
			Logger.Trace(message);
		}

		public override void WriteLine(string message)
		{
			Logger.Trace(message);
		}
	}

	/// <summary>
	/// A simple number generation class.
	/// </summary>
	public static class NumberGenerator
	{
		/// <summary>
		/// Generate a list of numbers around size edge cases.
		/// </summary>
		/// <param name="size">The size (in bits) of the data</param>
		/// <param name="n">The +/- range number</param>
		/// <returns>Returns a list of all sizes to be used</returns>
		public static long[] GenerateBadNumbers(int size, int n = 50)
		{
			if (size == 8)
				return BadNumbers8(n);
			if (size == 16)
				return BadNumbers16(n);
			if (size == 24)
				return BadNumbers24(n);
			if (size == 32)
				return BadNumbers32(n);
			if (size == 64)
				return BadNumbers64(n);
			throw new ArgumentOutOfRangeException("size");
		}

		public static long[] GenerateBadPositiveNumbers(int size = 16, int n = 50)
		{
			if (size == 16)
				return BadPositiveNumbers16(n);
			return null;
		}

		public static ulong[] GenerateBadPositiveUInt64(int n = 50)
		{
			var edgeCases = new ulong[] { 50, 127, 255, 32767, 65535, 2147483647, 4294967295, 9223372036854775807, 18446744073709551615 };
			var temp = new List<ulong>();

			ulong start;
			ulong end;
			for (var i = 0; i < edgeCases.Length - 1; ++i)
			{
				start = edgeCases[i] - (ulong)n;
				end = edgeCases[i] + (ulong)n;

				for (var j = start; j <= end; ++j)
					temp.Add(j);
			}

			start = edgeCases[8] - (ulong)n;
			end = edgeCases[8];
			for (var i = start; i < end; ++i)
				temp.Add(i);
			temp.Add(end);

			return temp.ToArray();
		}

		private static long[] BadNumbers8(int n)
		{
			var edgeCases = new long[] { 0, -128, 127, 255 };
			return Populate(edgeCases, n);
		}

		private static long[] BadNumbers16(int n)
		{
			var edgeCases = new long[] { 0, -128, 127, 255, -32768, 32767, 65535 };
			return Populate(edgeCases, n);
		}

		private static long[] BadNumbers24(int n)
		{
			var edgeCases = new long[] { 0, -128, 127, 255, -32768, 32767, 65535, -8388608, 8388607, 16777215 };
			return Populate(edgeCases, n);
		}

		private static long[] BadNumbers32(int n)
		{
			var edgeCases = new long[] { 0, -128, 127, 255, -32768, 32767, 65535, -2147483648, 2147483647, 4294967295 };
			return Populate(edgeCases, n);
		}

		private static long[] BadNumbers64(int n)
		{
			var edgeCases = new long[] { 0, -128, 127, 255, -32768, 32767, 65535, -2147483648, 2147483647, 4294967295, -9223372036854775808, 9223372036854775807 };    // UInt64.Max = 18446744073709551615;
			return Populate(edgeCases, n);
		}

		private static long[] BadPositiveNumbers16(int n)
		{
			var edgeCases = new long[] { 50, 127, 255, 32767, 65535 };
			return Populate(edgeCases, n);
		}

		private static long[] Populate(long[] values, int n)
		{
			var temp = new List<long>();

			for (var i = 0; i < values.Length; ++i)
			{
				var start = values[i] - n;
				var end = values[i] + n;

				for (var j = start; j <= end; ++j)
					temp.Add(j);
			}

			return temp.ToArray();
		}
	}

	[Serializable]
	public class HexString
	{
		public byte[] Value { get; private set; }

		private HexString(byte[] value)
		{
			Value = value;
		}

		public static HexString Parse(string s)
		{

			s = s.Replace(" ", "");
			if (s.Length % 2 == 0)
			{
				var array = ToArray(s);
				if (array != null)
					return new HexString(array);
			}

			throw new FormatException("An invalid hex string was specified.");
		}

		public static byte[] ToArray(string s)
		{
			if (s.Length % 2 != 0)
				throw new ArgumentException("s");

			var ret = new byte[s.Length / 2];

			for (var i = 0; i < s.Length; i += 2)
			{
				var nibble1 = GetNibble(s[i]);
				var nibble2 = GetNibble(s[i + 1]);

				if (nibble1 < 0 || nibble1 > 0xF || nibble2 < 0 | nibble2 > 0xF)
					return null;

				ret[i / 2] = (byte)((nibble1 << 4) | nibble2);
			}

			return ret;
		}

		private static int GetNibble(char c)
		{
			if (c >= 'a')
				return 0xA + (c - 'a');
			if (c >= 'A')
				return 0xA + (c - 'A');
			return c - '0';
		}
	}

	/// <summary>
	/// Some utility methods that be usefull
	/// </summary>
	public class Utilities
	{
		// Ensure trailing slash is stripped from ExecutionDirectory
		private static readonly string PeachDirectory =
			AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);

		/// <summary>
		/// Configure NLog.
		/// </summary>
		/// <remarks>
		/// Level &lt; 0 --&gt; Clear Config
		/// Level = 0 --&gt; Do nothing
		/// Level = 1 --&gt; Debug
		/// Leven &gt; 1 --&gt; Trace
		/// </remarks>
		/// <param name="level"></param>
		public static void ConfigureLogging(int level)
		{
			if (level < 0)
			{
				// Need to reset configuration to null for NLog 2.0 on mono
				// so we don't hang on exit.
				LogManager.Flush();
				LogManager.Configuration = null;
				return;
			}

			if (level == 0)
				return;

			if (LogManager.Configuration != null && LogManager.Configuration.LoggingRules.Count > 0)
			{
				Console.WriteLine("Logging was configured by a .config file, not changing the configuration.");
				return;
			}

			var nconfig = new LoggingConfiguration();
			var consoleTarget = new ConsoleTarget();
			nconfig.AddTarget("console", consoleTarget);
			consoleTarget.Layout = "${logger} ${message}";

			var rule = new LoggingRule("*", level == 1 ? LogLevel.Debug : LogLevel.Trace, consoleTarget);
			nconfig.LoggingRules.Add(rule);

			LogManager.Configuration = nconfig;
		}

		public static string FindProgram(string path, string program, string parameter)
		{
			var paths = path;
			if (string.IsNullOrEmpty(path))
			{
				paths = Environment.GetEnvironmentVariable("PATH");
			}
			Debug.Assert(!string.IsNullOrEmpty((paths)));
			var dirs = paths.Split(Path.PathSeparator);
			foreach (var dir in dirs)
			{
				var candidate = Path.Combine(dir, program);
				if (File.Exists(candidate))
					return candidate;
			}

			throw new PeachException("Error, unable to locate '{0}'{1} '{2}' parameter.".Fmt(
				program, path != null ? " in specified" : ", please specify using", parameter));
		}

		/// <summary>
		/// The location on disk where peach is executing from.
		/// Does not include the trailing slash in the directory name.
		/// </summary>
		public static string ExecutionDirectory
		{
			get { return PeachDirectory; }
		}

		/// <summary>
		/// Returns the name of the currently running executable.
		/// Equavilant to argv[0] in C/C++.
		/// </summary>
		public static string ExecutableName
		{
			get { return AppDomain.CurrentDomain.FriendlyName; }
		}

		public static string GetAppResourcePath(string resource)
		{
			return Path.Combine(ExecutionDirectory, resource);
		}

		public static string LoadStringResource(Assembly asm, string name)
		{
			using (var stream = asm.GetManifestResourceStream(name))
			{
				using (var reader = new StreamReader(stream, System.Text.Encoding.UTF8))
				{
					return reader.ReadToEnd();
				}
			}
		}

		public static void ExtractEmbeddedResource(Assembly asm, string name, string target)
		{
			var path = Path.Combine(ExecutionDirectory, target);
			using (var sout = new FileStream(path, FileMode.Create))
			{
				using (var sin = asm.GetManifestResourceStream(name))
				{
					sin.CopyTo(sout);
				}
			}
		}

		public static string FormatAsPrettyHex(byte[] data, int startPos = 0, int length = -1)
		{
			var sb = new StringBuilder();
			var rightSb = new StringBuilder();
			var lineLength = 15;
			var groupLength = 7;
			var gap = "  ";
			byte b;

			if (length == -1)
				length = data.Length;

			var cnt = 0;
			for (var i = startPos; i < data.Length && i < length; i++)
			{
				b = data[i];

				sb.Append(b.ToString("X2"));

				if (b >= 32 && b < 127)
					rightSb.Append(Encoding.ASCII.GetString(new byte[] { b }));
				else
					rightSb.Append(".");


				if (cnt == groupLength)
				{
					sb.Append("  ");
				}
				else if (cnt == lineLength)
				{
					sb.Append(gap);
					sb.Append(rightSb);
					sb.Append("\n");
					rightSb.Clear();

					cnt = -1; // (+1 happens later)
				}
				else
				{
					sb.Append(" ");
				}

				cnt++;
			}

			for (; cnt <= lineLength; cnt++)
			{
				sb.Append("  ");

				if (cnt == groupLength)
					sb.Append(" ");
				else if (cnt < lineLength)
				{
					sb.Append(" ");
				}
			}

			sb.Append(gap);
			sb.Append(rightSb);
			sb.Append("\n");
			rightSb.Clear();

			return sb.ToString();
		}

		public static bool TcpPortAvailable(int port)
		{
			var isAvailable = true;

			var ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
			var tcpConnInfoArray = ipGlobalProperties.GetActiveTcpConnections();

			foreach (var tcpi in tcpConnInfoArray)
			{
				if (tcpi.LocalEndPoint.Port == port)
				{
					isAvailable = false;
					break;
				}
			}

			var objEndPoints = ipGlobalProperties.GetActiveTcpListeners();

			foreach (var endp in objEndPoints)
			{
				if (endp.Port == port)
				{
					isAvailable = false;
					break;
				}
			}

			return isAvailable;
		}

		/// <summary>
		/// Compute the subrange resulting from diving a range into equal parts
		/// </summary>
		/// <param name="begin">Inclusive range begin</param>
		/// <param name="end">Inclusive range end</param>
		/// <param name="curSlice">The 1 based index of the current slice</param>
		/// <param name="numSlices">The total number of slices</param>
		/// <returns>Range of the current slice</returns>
		public static Tuple<uint, uint> SliceRange(uint begin, uint end, uint curSlice, uint numSlices)
		{
			if (begin > end)
				throw new ArgumentOutOfRangeException("begin");
			if (curSlice == 0 || curSlice > numSlices)
				throw new ArgumentOutOfRangeException("curSlice");

			var total = end - begin + 1;

			if (numSlices == 0 || numSlices > total)
				throw new ArgumentOutOfRangeException("numSlices");

			var slice = total / numSlices;

			end = curSlice * slice + begin - 1;
			begin = end - slice + 1;

			if (curSlice == numSlices)
				end += total % numSlices;

			return new Tuple<uint, uint>(begin, end);
		}

		// Slightly tweaked from:
		// http://www.codeproject.com/Articles/36747/Quick-and-Dirty-HexDump-of-a-Byte-Array
		private delegate void HexOutputFunc(char[] line);
		private delegate int HexInputFunc(byte[] buf, int max);

		private static void HexDump(HexInputFunc input, HexOutputFunc output, int bytesPerLine = 16)
		{
			var bytes = new byte[bytesPerLine];
			var HexChars = "0123456789ABCDEF".ToCharArray();

			var firstHexColumn =
				  8                   // 8 characters for the address
				+ 3;                  // 3 spaces

			var firstCharColumn = firstHexColumn
				+ bytesPerLine * 3       // - 2 digit for the hexadecimal value and 1 space
				+ (bytesPerLine - 1) / 8 // - 1 extra space every 8 characters from the 9th
				+ 2;                  // 2 spaces 

			var lineLength = firstCharColumn
				+ bytesPerLine           // - characters to show the ascii value
				+ Environment.NewLine.Length; // Carriage return and line feed (should normally be 2)

			var line = (new String(' ', lineLength - Environment.NewLine.Length) + Environment.NewLine).ToCharArray();

			for (var i = 0; ; i += bytesPerLine)
			{
				var readLen = input(bytes, bytesPerLine);
				if (readLen == 0)
					break;

				line[0] = HexChars[(i >> 28) & 0xF];
				line[1] = HexChars[(i >> 24) & 0xF];
				line[2] = HexChars[(i >> 20) & 0xF];
				line[3] = HexChars[(i >> 16) & 0xF];
				line[4] = HexChars[(i >> 12) & 0xF];
				line[5] = HexChars[(i >> 8) & 0xF];
				line[6] = HexChars[(i >> 4) & 0xF];
				line[7] = HexChars[(i >> 0) & 0xF];

				var hexColumn = firstHexColumn;
				var charColumn = firstCharColumn;

				for (var j = 0; j < bytesPerLine; j++)
				{
					if (j > 0 && (j & 7) == 0) hexColumn++;
					if (j >= readLen)
					{
						line[hexColumn] = ' ';
						line[hexColumn + 1] = ' ';
						line[charColumn] = ' ';
					}
					else
					{
						var b = bytes[j];
						line[hexColumn] = HexChars[(b >> 4) & 0xF];
						line[hexColumn + 1] = HexChars[b & 0xF];
						line[charColumn] = ((b < 32 || b > 126) ? '.' : (char)b);
					}
					hexColumn += 3;
					charColumn++;
				}

				output(line);
			}

		}

		public static void HexDump(Stream input, Stream output, int bytesPerLine = 16)
		{
			var pos = input.Position;

			HexInputFunc inputFunc = delegate(byte[] buf, int max)
			{
				return input.Read(buf, 0, max);
			};

			HexOutputFunc outputFunc = delegate(char[] line)
			{
				var buf = System.Text.Encoding.ASCII.GetBytes(line);
				output.Write(buf, 0, buf.Length);
			};

			HexDump(inputFunc, outputFunc, bytesPerLine);

			input.Seek(pos, SeekOrigin.Begin);
		}

		public static void HexDump(byte[] buffer, int offset, int count, Stream output, int bytesPerLine = 16)
		{
			HexInputFunc inputFunc = delegate(byte[] buf, int max)
			{
				var len = Math.Min(count, max);
				Buffer.BlockCopy(buffer, offset, buf, 0, len);
				offset += len;
				count -= len;
				return len;
			};

			HexOutputFunc outputFunc = delegate(char[] line)
			{
				var buf = System.Text.Encoding.ASCII.GetBytes(line);
				output.Write(buf, 0, buf.Length);
			};

			HexDump(inputFunc, outputFunc, bytesPerLine);
		}

		public static string HexDump(Stream input, int bytesPerLine = 16, int maxOutputSize = 1024*8)
		{
			var sb = new StringBuilder();
			var pos = input.Position;

			HexInputFunc inputFunc = (buf, max) =>
			{
				var len = input.Read(buf, 0, Math.Min(max, maxOutputSize));
				maxOutputSize -= len;
				return len;
			};

			HexOutputFunc outputFunc = line => sb.Append(line);

			HexDump(inputFunc, outputFunc, bytesPerLine);

			if (input.Position != input.Length)
				sb.AppendFormat("---- TRUNCATED (Total Length: {0} bytes) ----", input.Length);

			input.Seek(pos, SeekOrigin.Begin);

			return sb.ToString();
		}

		public static string HexDump(byte[] buffer, int offset, int count, int bytesPerLine = 16)
		{
			var sb = new StringBuilder();

			HexInputFunc inputFunc = delegate(byte[] buf, int max)
			{
				var len = Math.Min(count, max);
				Buffer.BlockCopy(buffer, offset, buf, 0, len);
				offset += len;
				count -= len;
				return len;
			};

			HexOutputFunc outputFunc = line => sb.Append(line);

			HexDump(inputFunc, outputFunc, bytesPerLine);

			return sb.ToString();
		}

		public static string PrettyBytes(long bytes)
		{
			if (bytes < 0)
				throw new ArgumentOutOfRangeException("bytes");

			if (bytes > (1024 * 1024 * 1024))
				return (bytes / (1024 * 1024 * 1024.0)).ToString("0.###") + " Gbytes";
			if (bytes > (1024 * 1024))
				return (bytes / (1024 * 1024.0)).ToString("0.###") + " Mbytes";
			if (bytes > 1024)
				return (bytes / 1024.0).ToString("0.###") + " Kbytes";
			return bytes + " Bytes";
		}
	}

	public class ToggleEventArgs : EventArgs
	{
		public bool Toggle { get; set; }
		public ToggleEventArgs(bool toggle)
		{
			Toggle = toggle;
		}
	}
}
