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
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

using Peach.Core;
using System.Collections.Specialized;
using System.Collections;

namespace Peach.Core.Analysis
{
# if DISABLED
	//public class Coverage
    /// <summary>
    /// Coverage implementation that utalizes PinTools
    /// </summary>
    public class CoverageImpl: Coverage
    {
		/// <summary>
		/// Temporary folder to store traces
		/// </summary>
        string _traceFolder = null;

		/// <summary>
		/// Pin tool dll by os and architecture.
		/// </summary>
		static Dictionary<Platform.OS, Dictionary<Platform.Architecture, string>> pinTool = new Dictionary<Platform.OS, Dictionary<Platform.Architecture, string>>();

		static CoverageImpl()
		{
			pinTool.Add(Platform.OS.Windows, new Dictionary<Platform.Architecture, string>());
			pinTool.Add(Platform.OS.Linux, new Dictionary<Platform.Architecture, string>());
			pinTool.Add(Platform.OS.OSX, new Dictionary<Platform.Architecture, string>());

			pinTool[Platform.OS.Windows][Platform.Architecture.x86] = @"bblocks32.dll";
			pinTool[Platform.OS.Windows][Platform.Architecture.x64] = @"bblocks64.dll";

			pinTool[Platform.OS.Linux][Platform.Architecture.x86] = @"bblocks32.so";
			pinTool[Platform.OS.Linux][Platform.Architecture.x64] = @"bblocks64.so";

			// OSX supports fat binaries
			pinTool[Platform.OS.OSX][Platform.Architecture.x86] = @"bblocks.dylib";
			pinTool[Platform.OS.OSX][Platform.Architecture.x64] = @"bblocks.dylib";
		}

        public CoverageImpl()
        {
            _traceFolder = Guid.NewGuid().ToString();
            while(Directory.Exists(_traceFolder))
                _traceFolder = Guid.NewGuid().ToString();

            Directory.CreateDirectory(_traceFolder);
		}

        /// <summary>
        /// Collect all basic blocks in binary
        /// </summary>
        /// <param name="executable"></param>
        /// <param name="needsKilling"></param>
        /// <returns></returns>
        public override List<ulong> BasicBlocksForExecutable(string executable, bool needsKilling)
        {
            return null;
        }

        /// <summary>
        /// Perform code coverage based on collection of basic blocks.  If
        /// not provided they will be generated by calling BasicBlocksForExecutable.
        /// </summary>
        /// <param name="executable"></param>
        /// <param name="arguments"></param>
        /// <param name="needsKilling"></param>
        /// <param name="basicBlocks"></param>
        /// <returns></returns>
        public override List<ulong> CodeCoverageForExecutable(string executable, string arguments, bool needsKilling, List<ulong> basicBlocks = null)
        {
            try
            {
                if (File.Exists("bblocks.out"))
                    File.Delete("bblocks.out");

				if (File.Exists("bblocks.pid"))
					File.Delete("bblocks.pid");

				// This is intended to disable this feature.
				// We currently want all trace files to be "masters" and not
				// diffs.  This will make it easy to distrubute out using
				// Peach Farm.
				if (File.Exists("bblocks.existing"))
					File.Delete("bblocks.existing");

				//if (basicBlocks != null && basicBlocks.Count > 0)
				//{
				//    StringBuilder sb = new StringBuilder();
				//    foreach (ulong block in basicBlocks)
				//        sb.AppendLine(block.ToString());

				//    File.WriteAllText("bblocks.existing", sb.ToString());
				//}

				var peachBinaries = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
				var tgtArch = FileInfo.Instance.GetArch(executable);
				var toolPath = Path.Combine(peachBinaries, pinTool[Platform.GetOS()][tgtArch]);

				if (!File.Exists(toolPath))
					throw new PeachException("Error, cannot locate pin tool '" + toolPath + "'.");

                var psi = new ProcessStartInfo();
				
				//psi.Arguments = "-t " + _pinTool + " -- " + executable + " " + arguments;
				psi.Arguments = string.Format("-t {0} -- {1} {2}", toolPath, executable, arguments);

				psi.FileName = Path.Combine(peachBinaries, "pin", "pin");
                psi.CreateNoWindow = true;
                psi.UseShellExecute = false;

				foreach (DictionaryEntry de in Environment.GetEnvironmentVariables())
					psi.EnvironmentVariables[de.Key.ToString()] = de.Value.ToString();

				if (Platform.GetOS() == Platform.OS.Linux)
				{
					var origin = Path.Combine(peachBinaries, "pin");
					var libs = "{0}/ia32/runtime:{0}/intel64/runtime:{0}/ia32/runtime/glibc:{0}/intel64/runtime/glibc".Fmt(origin);
					if (psi.EnvironmentVariables.ContainsKey("LD_LIBRARY_PATH"))
						libs += ":" + psi.EnvironmentVariables["LD_LIBRARY_PATH"].ToString();
					psi.EnvironmentVariables["LD_LIBRARY_PATH"] = libs;
				}

				using (var proc = new Process())
				{
					proc.StartInfo = psi;
					proc.Start();

					if (needsKilling)
					{
						while (!proc.HasExited && !File.Exists("bblocks.pid"))
							System.Threading.Thread.Sleep(100);

						int pid = 0;

						// Ensure pid is fully written
						while (true)
						{
							try
							{
								using (FileStream f = new FileStream("bblocks.pid", FileMode.Open, FileAccess.Read))
								{
									StreamReader rdr = new StreamReader(f);
									string contents = rdr.ReadToEnd();
									pid = Convert.ToInt32(contents);
									break;
								}
							}
							catch (IOException)
							{
							}
							catch
							{
								throw;
							}
						}

						try
						{
							using (var child = Process.GetProcessById(pid))
							{
								ulong lastTime = 0;
								ulong currTime = 0;
								const int pollInterval = 200;

								do
								{
									lastTime = currTime;
									System.Threading.Thread.Sleep(pollInterval);

									var pi = ProcessInfo.Instance.Snapshot(child);
									currTime = pi.TotalProcessorTicks;
								}
								while (lastTime != currTime);

								child.Kill();
							}
						}
						catch (ArgumentException)
						{
							// No such pid, must have already exited
						}
						catch (Exception ex)
						{
							Console.WriteLine();
							Console.WriteLine("Error waiting for idle cpu for '" + executable + "'.  " + ex.Message);
							proc.Kill();
						}
					}

					proc.WaitForExit();
				}

				if (!File.Exists("bblocks.out"))
				{
					Console.Error.WriteLine(psi.FileName);
					Console.Error.WriteLine(psi.Arguments);
				}

                List<ulong> blocks = new List<ulong>();
                using (StreamReader rin = new StreamReader("bblocks.out"))
                {
                    string line = rin.ReadLine();
                    while (!string.IsNullOrEmpty(line))
                    {
						try
						{
							blocks.Add(ulong.Parse(line));
							line = rin.ReadLine();
						}
						catch
						{
							Console.Error.WriteLine("[" + line + "]");
						}
                    }
                }

                return blocks;
            }
            finally
            {
                if (File.Exists("bblocks.out"))
                    File.Delete("bblocks.out");

				// See comments at top of function regarding
				// removing this file on every iteration.
				if (File.Exists("bblocks.existing"))
					File.Delete("bblocks.existing");
			}
        }

		public override void Dispose()
		{
			if (File.Exists("bblocks.existing"))
				File.Delete("bblocks.existing");

			if (_traceFolder != null && Directory.Exists(_traceFolder))
				Directory.Delete(_traceFolder, true);
		}
    }
#endif
}
