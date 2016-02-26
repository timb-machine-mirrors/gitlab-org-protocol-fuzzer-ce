using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.XPath;
using Peach.Core;
using Peach.Core.Analyzers;
using Peach.Core.Dom;
using Peach.Core.Dom.XPath;
using Peach.Core.Fixups;
using Peach.Core.IO;
using Peach.Pro.Core;
using Peach.Pro.Core.MutationStrategies;
using Peach.Pro.Core.WebServices.Models;
using Action = Peach.Core.Dom.Action;
using Ionic.Zip;
using StateModel = Peach.Core.Dom.StateModel;

namespace PitTester
{
	public class PitTester
	{
		public static void ExtractPack(string pack, string dir, int logLevel)
		{
			using (var zip = new ZipFile(pack))
			{
				Console.WriteLine("Extracting {0} to {1}", pack, dir);
		
				zip.ExtractProgress += (sender, e) => 
				{
					if (e.EventType == ZipProgressEventType.Extracting_BeforeExtractEntry)
					{
						var fileName = e.CurrentEntry.FileName;

						if (logLevel > 0)
							Console.WriteLine(fileName);
						
						if (fileName.EndsWith(".xml.config"))
						{
							var testFile = Path.ChangeExtension(fileName, ".test");
							var src = Path.Combine(Path.GetDirectoryName(pack), "Assets", testFile);
							var tgt = Path.Combine(dir, testFile);
							if (File.Exists(src))
							{
								Console.WriteLine(testFile);
								File.Copy(src, tgt);
							}
						}
					}
				};

				zip.ExtractAll(dir);
				Console.WriteLine();
			}
		}

		public static void OnIterationStarting(RunContext context, uint currentIteration, uint? totalIterations)
		{
			if (context.config.singleIteration)
				return;

			if (context.controlRecordingIteration)
				Console.Write('r');
			else if (context.controlIteration)
				Console.Write('c');
			else if ((currentIteration % 10) == 0)
				Console.Write(".");
		}

		public static void TestPit(string libraryPath, string pitFile, bool singleIteration, uint? seed, bool keepGoing, uint stop = 500)
		{
			var testFile = pitFile + ".test";
			if (!File.Exists(testFile))
				throw new FileNotFoundException();

			var testData = TestData.Parse(testFile);

			if (testData.Tests.Any(x => x.Skip))
				throw new FileNotFoundException();

			var cleanme = new List<IDisposable>();

			try
			{
				foreach (var tmp in testData.Defines.OfType<TestData.TempFileDefine>())
				{
					cleanme.Add(tmp);
					tmp.Populate();
				}

				DoTestPit(testData, libraryPath, pitFile, singleIteration, seed, keepGoing, stop);
			}
			finally
			{
				foreach (var item in cleanme)
					item.Dispose();
			}
		}

		private static void DoTestPit(TestData testData, string libraryPath, string pitFile, bool singleIteration, uint? seed, bool keepGoing, uint stop = 500)
		{
			if (testData.Tests.Any(x => x.SingleIteration))
				singleIteration = true;

			var defs = PitDefines.ParseFile(pitFile + ".config", libraryPath).Evaluate();

			var testDefs = testData.Defines.ToDictionary(x => x.Key, x => x.Value);

			for (var i = 0; i < defs.Count; ++i)
			{
				string value;
				if (testDefs.TryGetValue(defs[i].Key, out value))
				{
					if (value == defs[i].Value)
					{
						Console.WriteLine("Warning, .test and .config value are identical for PitDefine named: \"{0}\"",
							defs[i].Key
						);
					}

					defs[i] = new KeyValuePair<string, string>(defs[i].Key, value);
					testDefs.Remove(defs[i].Key);
				}
			}

			if (testDefs.Count > 0)
			{
				throw new PeachException("Error, PitDefine(s) in .test not found in .config: {0}".Fmt(
					string.Join(", ", testDefs.Keys))
				);
			}

			var args = new Dictionary<string, object>();
			args[PitParser.DEFINED_VALUES] = defs;

			var parser = new PitParser();

			var dom = parser.asParser(args, pitFile);

			var errors = new List<Exception>();
			var fixupOverrides = new Dictionary<string, Variant>();

			foreach (var test in dom.tests)
			{
				// Don't run extra control iterations...
				test.controlIteration = 0;

				test.agents.Clear();

				var data = testData.Tests.FirstOrDefault(t => t.Name == test.Name);
				if (data == null)
					throw new PeachException("Error, no test definition found for pit test named '{0}'.".Fmt(test.Name));

				var logger = new TestLogger(data, testData.Ignores.Select(i => i.Xpath));
				logger.Error += err =>
				{
					var ex = new PeachException(err);
					if (!keepGoing)
						throw ex;
					errors.Add(ex);
				};

				test.loggers.Clear();
				test.loggers.Add(logger);

				for (var i = 0; i < test.publishers.Count; ++i)
				{
					var oldPub = test.publishers[i];
					var newPub = new TestPublisher(logger, singleIteration) { Name = oldPub.Name };
					newPub.Error += err =>
					{
						var ex = new PeachException(err);
						if (!keepGoing)
							throw ex;
						errors.Add(ex);
					};
					test.publishers[i] = newPub;
				}

				if (testData.Slurps.Count > 0)
				{
					ApplySlurps(testData, test.stateModel, fixupOverrides);
				}
			}

			// See #214
			// If there are is any action that has more than one data set
			// that use <Field> and the random strategy is
			// in use, turn off data set switching...
			var noSwitch = dom.tests
				.Select(t => t.stateModel)
				.SelectMany(sm => sm.states)
				.SelectMany(s => s.actions)
				.SelectMany(a => a.allData)
				.Select(ad => ad.dataSets)
				.Any(ds => ds.SelectMany(d => d).OfType<DataField>().Count() > 1);

			if (noSwitch)
			{
				foreach (var t in dom.tests.Where(t => t.strategy is RandomStrategy))
				{
					t.strategy = new RandomStrategy(new Dictionary<string, Variant>
					{
						{ "SwitchCount", new Variant(int.MaxValue.ToString(CultureInfo.InvariantCulture)) },
					});
				}
			}

			var config = new RunConfiguration
			{
				range = true,
				rangeStart = 0,
				rangeStop = stop,
				pitFile = Path.GetFileName(pitFile),
				runName = "Default",
				singleIteration = singleIteration
			};

			if (seed.HasValue)
				config.randomSeed = seed.Value;

			var q = testData.Tests.First(i => i.Name == config.runName);
			if (!string.IsNullOrEmpty(q.Seed))
			{
				uint s;
				if (!uint.TryParse(q.Seed, out s))
					throw new PeachException("Error, could not parse test seed '{0}' as an unsigned integer.".Fmt(q.Seed));

				config.randomSeed = s;
			}


			uint num = 0;
			var e = new Engine(null);
			e.IterationStarting += (ctx, it, tot) => num = it;

			e.TestStarting += ctx =>
			{
				foreach (var kv in fixupOverrides)
					ctx.stateStore.Add(kv.Key, kv.Value);

				if (testData.Slurps.Count > 0)
				{
					ctx.StateModelStarting += (context, model) =>
					{
						ApplySlurps(testData, model, null);
					};
				}
			};

			e.IterationStarting += (ctx, it, tot) => num = it;

			try
			{
				e.startFuzzing(dom, config);
			}
			catch (Exception ex)
			{
				var msg = "Encountered an unhandled exception on iteration {0}, seed {1}.\n{2}".Fmt(
					num,
					config.randomSeed,
					ex.Message);
				errors.Add(new PeachException(msg, ex));
			}

			if (errors.Any())
				throw new AggregateException(errors);
		}

		private static void ApplySlurps(TestData testData, StateModel sm, Dictionary<string, Variant> fixupOverrides)
		{
			var doc = new XmlDocument();
			var resolver = new PeachXmlNamespaceResolver();
			var navi = new PeachXPathNavigator(sm);

			foreach (var slurp in testData.Slurps)
			{
				var iter = navi.Select(slurp.SetXpath, resolver);
				if (!iter.MoveNext())
					throw new SoftException("Error, slurp valueXpath returned no values. [" + slurp.SetXpath + "]");

				var n = doc.CreateElement("Foo");
				n.SetAttribute("valueType", slurp.ValueType);
				n.SetAttribute("value", slurp.Value);

				var blob = new Blob();
				new PitParser().handleCommonDataElementValue(n, blob);

				do
				{
					var setElement = ((PeachXPathNavigator)iter.Current).CurrentNode as DataElement;
					if (setElement == null)
						throw new PeachException("Error, slurp setXpath did not return a Data Element. [" + slurp.SetXpath + "]");

					setElement.DefaultValue = blob.DefaultValue;

					if (fixupOverrides != null && setElement.fixup is VolatileFixup)
					{
						var dm = setElement.root as DataModel;
						if (dm != null && dm.actionData != null)
						{
							// If the element is under an action, and has a volatile fixup
							// store off the value for overriding during TestStarting
							var key = "Peach.VolatileOverride.{0}.{1}".Fmt(dm.actionData.outputName, setElement.fullName);
							fixupOverrides[key] = blob.DefaultValue;
						}
					}

					if (blob.DefaultValue.GetVariantType() == Variant.VariantType.BitStream)
						((BitwiseStream)blob.DefaultValue).Position = 0;
				} while (iter.MoveNext());
			}
		}

		public static void ProfilePit(string pitLibraryPath, string fileName)
		{
			var defs = PitDefines.ParseFile(fileName + ".config", pitLibraryPath).Evaluate();

			var args = new Dictionary<string, object>();
			args[PitParser.DEFINED_VALUES] = defs;

			var parser = new PitParser();

			var dom = parser.asParser(args, fileName);

			dom.context = new RunContext();

			foreach (var test in dom.tests)
			{
				dom.context.test = test;

				foreach (var state in test.stateModel.states)
				{
					foreach (var action in state.actions)
					{
						foreach (var actionData in action.allData)
						{
							foreach (var data in actionData.allData.OfType<DataFile>())
							{
								var dm = actionData.dataModel;

								for (var i = 0; i < 1000; ++i)
								{
									var clone = (DataModel)dm.Clone();
									clone.actionData = actionData;
									data.Apply(clone);
								}

								return;
							}
						}
					}
				}
			}
		}

		public static void VerifyDataSets(string pitLibraryPath, string fileName)
		{
			var testData = new TestData();

			var pitTest = fileName + ".test";
			if (File.Exists(pitTest))
				testData = TestData.Parse(pitTest);

			var cleanme = new List<IDisposable>();

			try
			{
				foreach (var tmp in testData.Defines.OfType<TestData.TempFileDefine>())
				{
					cleanme.Add(tmp);
					tmp.Populate();
				}

				DoVerifyDataSets(testData, pitLibraryPath, fileName);
			}
			finally
			{
				foreach (var item in cleanme)
					item.Dispose();
			}
		}

		private static void DoVerifyDataSets(TestData testData, string pitLibraryPath, string fileName)
		{
			var defs = PitDefines.ParseFileWithDefaults(pitLibraryPath, fileName);

			var testDefs = testData.Defines.ToDictionary(x => x.Key, x => x.Value);

			for (var i = 0; i < defs.Count; ++i)
			{
				string value;
				if (testDefs.TryGetValue(defs[i].Key, out value))
					defs[i] = new KeyValuePair<string, string>(defs[i].Key, value);
			}

			var args = new Dictionary<string, object>();
			args[PitParser.DEFINED_VALUES] = defs;

			var parser = new PitParser();

			var dom = parser.asParser(args, fileName);

			dom.context = new RunContext();

			var sb = new StringBuilder();

			foreach (var test in dom.tests)
			{
				dom.context.test = test;

				var testTest = testData.Tests.FirstOrDefault(t => t.Name == test.Name);

				foreach (var state in test.stateModel.states)
				{
					foreach (var action in state.actions)
					{
						foreach (var actionData in action.allData)
						{
							foreach (var data in actionData.allData)
							{
								var verify = testTest == null || testTest.VerifyDataSets;
								VerifyDataSet(verify, data, actionData, test, state, action, sb);
							}
						}
					}
				}
			}

			if (sb.Length > 0)
				throw new PeachException(sb.ToString());
		}

		private static void VerifyDataSet(
			bool verifyBytes, 
			Data data, 
			ActionData actionData, 
			Test test, 
			State state,
			Action action, 
			StringBuilder sb)
		{
			try
			{
				if (data is DataFile)
				{
					// Verify file cracks correctly
					try
					{
						actionData.Apply(data);
					}
					catch (Exception ex)
					{
						throw new PeachException(string.Format("Error cracking data file '{0}' to '{1}.{2}.{3}.{4}'.",
							((DataFile) data).FileName, test.Name, state.Name, action.Name, actionData.dataModel.Name), ex);
					}

					// SHould we skip verifying bytes?
					if (!verifyBytes)
						return;

					var bs = actionData.dataModel.Value;
					var value = new MemoryStream();
					bs.Seek(0, SeekOrigin.Begin);
					bs.CopyTo(value);
					value.Seek(0, SeekOrigin.Begin);

					var dataFileBytes = File.ReadAllBytes(((DataFile) data).FileName);

					// Verify all bytes match
					for (var i = 0; i < dataFileBytes.Length && i < value.Length; i++)
					{
						var b = value.ReadByte();
						if (dataFileBytes[i] != b)
						{
							throw new PeachException(
								string.Format(
									"Error: Data did not match at {0}.  Got {1:x2} expected {2:x2}. Data file '{3}' to '{4}.{5}.{6}.{7}'.",
									i, b, dataFileBytes[i], ((DataFile) data).FileName, test.Name, state.Name, action.Name,
									actionData.dataModel.Name));
						}
					}

					// Verify length matches
					if (dataFileBytes.Length != value.Length)
						throw new PeachException(
							string.Format(
								"Error: Data size mismatch. Got {0} bytes, expected {1}. Data file '{2}' to '{3}.{4}.{5}.{6}'.",
								value.Length, dataFileBytes.Length, ((DataFile) data).FileName, test.Name, state.Name, action.Name,
								actionData.dataModel.Name));
				}
				else if (data is DataField)
				{
					// Verify fields apply correctly
					try
					{
						actionData.Apply(data);
					}
					catch (Exception ex)
					{
						throw new PeachException(string.Format("Error applying data fields '{0}' to '{1}.{2}.{3}.{4}'.\n{5}",
							data.Name, test.Name, state.Name, action.Name, actionData.dataModel.Name, ex.Message), ex);
					}
				}
			}
			catch (Exception ಠ_ಠ)
			{
				sb.AppendLine(ಠ_ಠ.Message);
			}
		}
	}
}
