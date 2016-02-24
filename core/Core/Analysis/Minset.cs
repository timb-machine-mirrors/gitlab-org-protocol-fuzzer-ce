
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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NLog;

namespace Peach.Core.Analysis
{
	public delegate void TraceEventHandler(object sender, string fileName, int count, int totalCount);
	public delegate void TraceEventMessage(object sender, string message);

	/// <summary>
	/// Perform analysis on sample sets to identify the smallest sample set
	/// that provides the largest code coverage.
	/// </summary>
	public class Minset
	{
		private static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();

		public event TraceEventHandler TraceStarting;
		public event TraceEventHandler TraceCompleted;
		public event TraceEventHandler TraceFailed;
		public event TraceEventMessage TraceMessage;

		protected void OnTraceStarting(string fileName, int count, int totalCount)
		{
			if (TraceStarting != null)
				TraceStarting(this, fileName, count, totalCount);
		}

		public void OnTraceCompleted(string fileName, int count, int totalCount)
		{
			if (TraceCompleted != null)
				TraceCompleted(this, fileName, count, totalCount);
		}

		public void OnTraceFaled(string fileName, int count, int totalCount)
		{
			if (TraceFailed != null)
				TraceFailed(this, fileName, count, totalCount);
		}

		private void ValidateTraces(List<string> samples, List<string> traces)
		{
			samples.Sort(string.CompareOrdinal);
			traces.Sort(string.CompareOrdinal);

			var i = 0;

			while (i < samples.Count || i < traces.Count)
			{
				int cmp;

				if (i == samples.Count)
					cmp = 1;
				else if (i == traces.Count)
					cmp = -1;
				else
					cmp = string.CompareOrdinal(Path.GetFileName(samples[i] + ".trace"), Path.GetFileName(traces[i]));

				if (cmp < 0)
				{
					if (TraceMessage != null)
						TraceMessage(this, "Ignoring sample '{0}' becaues of mising trace file.".Fmt(samples[i]));

					Logger.Debug("Ignoring sample '{0}' becaues of mising trace file.".Fmt(samples[i]));

					samples.RemoveAt(i);
				}
				else if (cmp > 0)
				{
					if (TraceMessage != null)
						TraceMessage(this, "Ignoring trace '{0}' becaues of mising sample file.".Fmt(traces[i]));

					Logger.Debug("Ignoring trace '{0}' becaues of mising sample file.".Fmt(traces[i]));

					traces.RemoveAt(i);
				}
				else
				{
					++i;
				}
			}
		}

		/// <summary>
		/// Perform coverage analysis of trace files.
		/// </summary>
		/// <remarks>
		/// Note: The sample and trace collections must have matching indexes.
		/// </remarks>
		/// <param name="sampleFiles">Collection of sample files</param>
		/// <param name="traceFiles">Collection of trace files for sample files</param>
		/// <returns>Returns the minimum set of smaple files.</returns>
		public string[] RunCoverage(string [] sampleFiles, string [] traceFiles)
		{
			var samples = sampleFiles.ToList();
			var traces = traceFiles.ToList();

			// Expect samples and traces to correlate 1 <-> 1
			ValidateTraces(samples, traces);

			Debug.Assert(samples.Count == traces.Count);

			var coverage = new Dictionary<string, long>();
			var db = new Dictionary<string, HashSet<string>>();

			if (TraceMessage != null)
				TraceMessage(this, "Loading {0} trace files...".Fmt(traces.Count));

			for (var i = 0; i < traces.Count; ++i)
			{
				var trace = traces[i];
				var sample = samples[i];

				Logger.Debug("Loading '{0}'", trace);

				coverage.Add(sample, 0);

				using (var rdr = new StreamReader(trace))
				{
					string line;

					while ((line = rdr.ReadLine()) != null)
					{
						HashSet<string> v;

						if (db.TryGetValue(line, out v))
						{
							v.Add(sample);
						}
						else
						{
							db.Add(line, new HashSet<string> { sample });
						}
					}
				}
			}

			if (TraceMessage != null)
				TraceMessage(this, "Computing minimum set coverage...".Fmt(traces.Count));

			Logger.Debug("Loaded {0} files, starting minset computation", traces.Count);

			var total = db.Count;
			var ret = new List<string>();

			while (coverage.Count > 0)
			{
				// Find trace with greatest coverage
				foreach (var t in db.SelectMany(row => row.Value))
					coverage[t] += 1;

				var max = coverage.Max(kv => kv.Value);
				var keep = coverage.First(kv => kv.Value == max).Key;

				if (max == 0)
					break;

				Logger.Debug("Keeping '{0}' with coverage {1}/{2}", keep, max, total);

				if (max < 10)
				{
					foreach (var l in db.Where(kv => kv.Value.Contains(keep)))
						Logger.Debug(l.Key);
				}

				ret.Add(keep);

				// Don't track selected trace anymore
				coverage.Remove(keep);

				// Reset coverage counts to 0
				foreach (var k in coverage.Keys.ToList())
					coverage[k] = 0;

				// Select all rows that are now covered
				var prune = db
					.Where(kv => kv.Value.Remove(keep))
					.Select(kv => kv.Key)
					.ToList();

				// Remvoe all covered rows
				foreach (var p in prune)
					db.Remove(p);
			}

			Logger.Debug("Removing {0} sample files", coverage.Count);

			foreach (var kv in coverage)
				Logger.Debug(" - {0}", kv.Key);

			Logger.Debug("Done");

			return ret.ToArray();
		}

		/// <summary>
		/// Collect traces for a collection of sample files.
		/// </summary>
		/// <remarks>
		/// This method will use the TraceStarting and TraceCompleted events
		/// to report progress.
		/// </remarks>
		/// <param name="executable">Executable to run.</param>
		/// <param name="arguments">Executable arguments.  Must contain a "%s" placeholder for the sampe filename.</param>
		/// <param name="tracesFolder">Where to write trace files</param>
		/// <param name="sampleFiles">Collection of sample files</param>
		/// <param name="needsKilling">Does this command requiring forcefull killing to exit?</param>
		/// <returns>Returns a collection of trace files</returns>
		public string[] RunTraces(string executable, string arguments, string tracesFolder, string[] sampleFiles, bool needsKilling = false)
		{
			try
			{
				var cov = new Coverage(executable, arguments, needsKilling);
				var ret = new List<string>();

				for (var i = 0; i < sampleFiles.Length; ++i)
				{
					var sampleFile = sampleFiles[i];
					var traceFile = Path.Combine(tracesFolder, Path.GetFileName(sampleFile) + ".trace");

					Logger.Debug("Starting trace [{0}:{1}] {2}", i + 1, sampleFiles.Length, sampleFile);

					OnTraceStarting(sampleFile, i + 1, sampleFiles.Length);

					try
					{
						cov.Run(sampleFile, traceFile);
						ret.Add(traceFile);
						Logger.Debug("Successfully created trace {0}", traceFile);
						OnTraceCompleted(sampleFile, i + 1, sampleFiles.Length);
					}
					catch (Exception ex)
					{
						Logger.Debug("Failed to generate trace.\n{0}", ex);
						OnTraceFaled(sampleFile, i + 1, sampleFiles.Length);
					}
				}

				return ret.ToArray();
			}
			catch (Exception ex)
			{
				Logger.Debug("Failed to create coverage.\n{0}", ex);

				throw new PeachException(ex.Message, ex);
			}
		}
	}
}
