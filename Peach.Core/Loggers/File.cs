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
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Peach.Core;
using Peach.Core.Agent;
using Peach.Core.Dom;

namespace Peach.Core.Loggers
{
	/// <summary>
	/// Standard file system logger.
	/// </summary>
	[Logger("File")]
	[Logger("Filesystem", true)]
	[Logger("logger.Filesystem")]
	[Parameter("Path", typeof(string), "Log folder")]
	public class FileLogger : Logger
	{
		string logpath = null;
		string ourpath = null;
		TextWriter log = null;

		public FileLogger(Dictionary<string, Variant> args)
		{
			logpath = (string)args["Path"];
		}

		public string Path
		{
			get { return logpath; }
		}

		protected override void Engine_Fault(RunContext context, uint currentIteration, StateModel stateModel,
			Fault [] faults)
		{
            Fault coreFault = null;
            List<Fault> dataFaults = new List<Fault>();

			log.WriteLine("! Fault detected at iteration {0} : {1}", currentIteration, DateTime.Now.ToString());

            // First find the core fault.
            foreach (Fault fault in faults)
            {
                if (fault.type == FaultType.Fault)
                    coreFault = fault;
                else
                    dataFaults.Add(fault);
            }

            if (coreFault == null)
                throw new ApplicationException("Error, we should always have a fault with type = Fault!");

			string faultPath = System.IO.Path.Combine(ourpath, "Faults");
			if (!Directory.Exists(faultPath))
				Directory.CreateDirectory(faultPath);

            if(coreFault.folderName != null)
                faultPath = System.IO.Path.Combine(faultPath, coreFault.folderName);

            else if (coreFault.majorHash == null && coreFault.minorHash == null && coreFault.exploitability == null)
            {
                faultPath = System.IO.Path.Combine(faultPath, "Unknown");
            }
            else
            {
                faultPath = System.IO.Path.Combine(faultPath,
                    string.Format("{0}_{1}_{2}", coreFault.exploitability, coreFault.majorHash, coreFault.minorHash));
            }

			if (!Directory.Exists(faultPath))
				Directory.CreateDirectory(faultPath);

			faultPath = System.IO.Path.Combine(faultPath, currentIteration.ToString());
			if (!Directory.Exists(faultPath))
				Directory.CreateDirectory(faultPath);

			int cnt = 0;
			foreach (Dom.Action action in stateModel.dataActions)
			{
				cnt++;
				if (action.dataModel != null)
				{
					string fileName = System.IO.Path.Combine(faultPath, string.Format("action_{0}_{1}_{2}.txt",
								  cnt, action.type.ToString(), action.name));

					File.WriteAllBytes(fileName, action.dataModel.Value.Value);
				}
				else if (action.parameters.Count > 0)
				{
					int pcnt = 0;
					foreach (Dom.ActionParameter param in action.parameters)
					{
						pcnt++;
						string fileName = System.IO.Path.Combine(faultPath, string.Format("action_{0}-{1}_{2}_{3}.txt",
										cnt, pcnt, action.type.ToString(), action.name));

						File.WriteAllBytes(fileName, param.dataModel.Value.Value);
					}
				}
			}

            // Write out all data information
            foreach (Fault fault in faults)
            {
                foreach(string key in fault.collectedData.Keys)
                {
                    string fileName = System.IO.Path.Combine(faultPath, 
                        fault.detectionSource + "_" + key);
                    File.WriteAllBytes(fileName, fault.collectedData[key]);
                }

                if (fault.description != null)
                {
                    string fileName = System.IO.Path.Combine(faultPath,
                        fault.detectionSource + "_" + "description.txt");
                    File.WriteAllText(fileName, fault.description);
                }
            }
		}

		protected override void Engine_IterationStarting(RunContext context, uint currentIteration, uint? totalIterations)
		{
			if (currentIteration != 1 && currentIteration % 100 != 0)
				return;

			if (totalIterations != null)
			{
				log.WriteLine(". Iteration {0} of {1} : {2}", currentIteration, (uint)totalIterations, DateTime.Now.ToString());
				log.Flush();
			}
			else
			{
				log.WriteLine(". Iteration {0} : {1}", currentIteration, DateTime.Now.ToString());
				log.Flush();
			}
		}

		protected override void Engine_TestError(RunContext context, Exception e)
		{
			log.WriteLine("! Test error: " + e.ToString());
			log.Flush();
		}

		protected override void Engine_TestFinished(RunContext context)
		{
			log.WriteLine(". Test finished: " + context.test.name);
			log.Flush();

			if (log != null)
			{
				log.Flush();
				log.Close();
				log.Dispose();
				log = null;
			}
		}

		protected override void Engine_TestStarting(RunContext context)
		{
			if (log != null)
			{
				log.Flush();
				log.Close();
				log.Dispose();
				log = null;
			}

			if (!Directory.Exists(logpath))
				Directory.CreateDirectory(logpath);

			ourpath = System.IO.Path.Combine(logpath, context.config.pitFile.Replace(System.IO.Path.DirectorySeparatorChar, '_'));

			if (context.config.runName == "DefaultRun")
				ourpath += "_" + string.Format("{0:yyyyMMddhhmmss}", DateTime.Now);
			else
				ourpath += "_" + context.config.runName + "_" + string.Format("{0:yyyyMMddhhmmss}", DateTime.Now);

			Directory.CreateDirectory(ourpath);

			log = File.CreateText(System.IO.Path.Combine(ourpath, "status.txt"));

			log.WriteLine("Peach Fuzzing Run");
			log.WriteLine("=================");
			log.WriteLine("");
			log.WriteLine("Date of run: " + context.config.runDateTime.ToString());
			log.WriteLine("Peach Version: " + context.config.version);

			log.WriteLine("Seed: " + context.config.randomSeed);

			log.WriteLine("Command line: " + context.config.commandLine);
			log.WriteLine("Pit File: " + context.config.pitFile);
			log.WriteLine(". Test starting: " + context.test.name);
			log.WriteLine("");

			log.Flush();
		}
	}
}

// end

