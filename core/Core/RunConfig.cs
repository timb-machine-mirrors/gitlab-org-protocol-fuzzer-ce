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
using System.Reflection;

namespace Peach.Core
{
	/// <summary>
	/// Configure the current run
	/// </summary>
	[Serializable]
	public class RunConfiguration
	{
		/// <summary>
		/// A unique identifier for this run
		/// </summary>
		public Guid id
		{
			get
			{
				if (!_id.HasValue)
					_id = Guid.NewGuid();
				return _id.Value;
			}
			set { _id = value; }
		}

		/// <summary>
		/// Just get the count of mutations
		/// </summary>
		public bool countOnly = false;

		/// <summary>
		/// Perform a single iteration
		/// </summary>
		public bool singleIteration = false;

		/// <summary>
		/// Specify the test range to perform
		/// </summary>
		public bool range = false;
		public uint rangeStart = 0;
		public uint rangeStop = 0;

		/// <summary>
		/// Controls parallel fuzzing
		/// </summary>
		public bool parallel = false;
		public uint parallelNum = 0;
		public uint parallelTotal = 0;

		/// <summary>
		/// Skip to a specific iteration
		/// </summary>
		public uint skipToIteration;

		/// <summary>
		/// Enable or disable debugging output
		/// </summary>
		[Obsolete]
		public int debug { get; set; }

		/// <summary>
		/// Name of run to perform
		/// </summary>
		public string runName = "Default";

		/// <summary>
		/// Name of PIT file (used by logger)
		/// </summary>
		public string pitFile = null;

		/// <summary>
		/// Command line if any (used by logger)
		/// </summary>
		public string[] commandLine = new string[0];

		/// <summary>
		/// Date and time of run (used by logger)
		/// </summary>
		public DateTime runDateTime = DateTime.Now;

		/// <summary>
		/// How long to run the fuzzer for.
		/// </summary>
		public TimeSpan Duration = TimeSpan.MaxValue;

		/// <summary>
		/// How long to wait for the engine to stop before giving up and aborting.
		/// </summary>
		public TimeSpan AbortTimeout = TimeSpan.FromMinutes(1);

		/// <summary>
		/// Random number generator SEED
		/// </summary>
		/// <remarks>
		/// If the same SEED value is specified the same
		/// iterations will be performed with same values.
		/// </remarks>
		public uint randomSeed
		{
			get
			{
				return _randomSeed;
			}
			set
			{
				_randomSeed = value;
				userDefinedSeed = true;
			}
		}

		/// <summary>
		/// Was randomSeed set by the user.
		/// </summary>
		public bool userDefinedSeed
		{
			get;
			private set;
		}

		/// <summary>
		/// Peach version currently running (used by logger)
		/// </summary>
		public string version
		{
			get
			{
				return Assembly.GetExecutingAssembly().GetName().Version.ToString();
			}
		}
		
		/// <summary>
		/// Function that returns true if the engine should stop
		/// </summary>
		public delegate bool StopHandler();
		
		/// <summary>
		/// Called every iteration by the engine to check if it should stop
		/// </summary>
		public StopHandler shouldStop = null;

		private uint _randomSeed = (uint)DateTime.Now.Ticks & 0x0000FFFF;
		private Guid? _id;
	}
}

// end
