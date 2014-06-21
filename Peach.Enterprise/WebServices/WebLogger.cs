using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.IO;
using Peach.Enterprise.WebServices.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Peach.Enterprise.WebServices
{
	public class WebLogger : Logger
	{
		Core.Fault[] reproFault;

		List<FaultSummary> faults = new List<FaultSummary>();
		Dictionary<string, FaultDetail> faultDetails = new Dictionary<string, FaultDetail>();

		Visualizer visualizer; // Cached JSON data
		VizData vizDataStart;  // Partial data, collecting during current iteration
		VizData vizDataFinal;  // Complete data, collected during last iteration

		#region Visualizer Data Helper Class

		class VizData
		{
			class Context
			{
				public string OutputName { get; set; }
				public DataModel OriginalDataModel { get; set; }
				public DataModel DataModel { get; set; }
			}

			uint Iteration { get; set; }
			List<string> MutatedElements { get; set; }
			List<Context> OutputData { get; set; }

			public VizData(uint iteration)
			{
				Iteration = iteration;
				MutatedElements = new List<string>();
				OutputData = new List<Context>();
			}

			public Visualizer MakeViz(Visualizer current)
			{
				if (current != null && current.Iteration == Iteration)
					return current;

				var ret = new Visualizer()
				{
					Iteration = Iteration,
					MutatedElements = MutatedElements,
					Models = new List<Visualizer.Model>(),
				};

				foreach (var item in OutputData)
				{
					var m = MakeModel(item);
					ret.Models.Add(m);
				}

				return ret;
			}

			public void DataMutating(ActionData actionData, DataElement element)
			{
				MutatedElements.Add(actionData.outputName + "." + element.fullName);
			}

			public void ActionFinished(Action action)
			{
				foreach (var item in action.outputData)
				{
					// The action can reset its DataModel if it is re-entered
					// so we need to keep a ref to item.DataModel, not item
					var rec = new VizData.Context()
					{
						OutputName = item.outputName,
						OriginalDataModel = item.originalDataModel,
						DataModel = item.dataModel,
					};

					OutputData.Add(rec);
				}
			}

			static byte[] ToBytes(BitwiseStream bs)
			{
				var padded = bs.PadBits();
				padded.Seek(0, System.IO.SeekOrigin.Begin);
				return new BitReader(padded).ReadBytes((int)padded.Length);
			}

			static Visualizer.Model MakeModel(Context ctx)
			{
				var ret = new Visualizer.Model()
				{
					Original = ToBytes(ctx.OriginalDataModel.Value),
					Fuzzed = ToBytes(ctx.DataModel.Value),
				};

				var toVisit = new List<System.Tuple<DataElement, Visualizer.Element>>();
				toVisit.Add(null);

				var it = new System.Tuple<DataElement, Visualizer.Element>(ctx.DataModel, ret);

				while (it != null)
				{
					it.Item2.Name = it.Item1.name;
					it.Item2.Type = it.Item1.elementType;

					int index = toVisit.Count;

					var cont = it.Item1 as DataElementContainer;
					if (cont != null)
					{
						it.Item2.Children = new List<Visualizer.Element>();

						foreach (var item in cont)
						{
							var child = new Visualizer.Element();
							it.Item2.Children.Add(child);
							toVisit.Insert(index, new System.Tuple<DataElement, Visualizer.Element>(item, child));
						}
					}

					index = toVisit.Count - 1;
					it = toVisit[index];
					toVisit.RemoveAt(index);
				}

				ret.Name = ctx.OutputName + "." + ctx.DataModel.name;

				return ret;
			}
		}

		#endregion

		public PitTester Tester { get; set; }

		public Thread Thread { get; private set; }
		public string NodeGuid { get; private set; }
		public string JobGuid { get; private set; }
		public string Name { get; private set; }
		public uint Seed { get; private set; }
		public uint CurrentIteration { get; private set; }
		public uint StartIteration { get; private set; }
		public uint FaultCount { get; private set; }
		public System.DateTime StartDate { get; private set; }

		public bool Busy
		{
			get
			{
				if (JobGuid != null || Thread != null)
					return true;

				return Tester != null && Tester.Status == TestStatus.Active;
			}
		}

		/// <summary>
		/// Get the most recent visualizer snapshot.
		/// </summary>
		public Visualizer Visualizer
		{
			get
			{
				lock (this)
				{
					// Update the cached view using the last view and vizDataFinal
					visualizer = vizDataFinal.MakeViz(visualizer);

					// Return the cached visualizer view
					return visualizer;
				}
			}
		}

		/// <summary>
		/// Get a list of the summary of all detected faults.
		/// </summary>
		public List<FaultSummary> Faults
		{
			get
			{
				lock (this)
				{
					// Return a copy of our list
					return faults.ToList();
				}
			}
		}

		/// <summary>
		/// Get the details of a specific fault id.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public FaultDetail GetFault(string id)
		{
			lock (this)
			{
				FaultDetail ret;
				faultDetails.TryGetValue(id, out ret);
				return ret;
			}
		}

		public void RunJob(string pitLibraryPath, string pitFile)
		{
		}

		public WebLogger()
		{
			NodeGuid = System.Guid.NewGuid().ToString().ToLower();
		}

		protected override void Engine_TestStarting(RunContext context)
		{
			lock (this)
			{
				JobGuid = System.Guid.NewGuid().ToString().ToLower();
				Name = context.config.pitFile;
				Seed = context.config.randomSeed;
				CurrentIteration = context.currentIteration;
				CurrentIteration = StartIteration;
				FaultCount = 0;
				StartDate = context.config.runDateTime.ToUniversalTime();

				visualizer = null;
				vizDataStart = null;
				vizDataFinal = new VizData(CurrentIteration);
			}
		}

		protected override void Engine_TestFinished(RunContext context)
		{
			lock (this)
			{
				JobGuid = null;
				Name = null;
				Seed = 0;
				CurrentIteration = 0;
				StartIteration = 0;
				FaultCount = 0;
				StartDate = System.DateTime.MinValue;

				visualizer = null;
				vizDataStart = null;
				vizDataFinal = null;

				faults.Clear();
				faultDetails.Clear();
			}
		}

		protected override void Engine_IterationStarting(RunContext context, uint currentIteration, uint? totalIterations)
		{
			CurrentIteration = currentIteration;

			vizDataStart = new VizData(currentIteration);
		}

		protected override void Engine_IterationFinished(RunContext context, uint currentIteration)
		{
			vizDataFinal = vizDataStart;
		}

		protected override void ActionStarting(RunContext context, Action action)
		{
		}

		protected override void ActionFinished(RunContext context, Action action)
		{
			vizDataStart.ActionFinished(action);
		}

		protected override void DataMutating(RunContext context, ActionData actionData, DataElement element, Mutator mutator)
		{
			vizDataStart.DataMutating(actionData, element);
		}

		protected override void Engine_ReproFault(RunContext context, uint currentIteration, StateModel stateModel, Core.Fault[] faultData)
		{
			// Caught fault, trying to reproduce
			reproFault = faultData;
		}

		protected override void Engine_Fault(RunContext context, uint currentIteration, StateModel stateModel, Core.Fault[] faultData)
		{
			// Caught fault
			OnFault(context, faultData, reproFault);
		}

		protected override void Engine_ReproFailed(RunContext context, uint currentIteration)
		{
			// Could not reproduce fault
			OnFault(context, reproFault, null);
		}

		void OnFault(RunContext context, Core.Fault[] faultData, Core.Fault[] initialData)
		{
			// Reset any pending fault we are trying to reproduce
			reproFault = null;

			var coreFault = faultData.First(f => f.type == FaultType.Fault);

			var id = System.Guid.NewGuid().ToString();
			var bucket = string.Join("_", new[] { coreFault.majorHash, coreFault.minorHash, coreFault.exploitability }.Where(s => !string.IsNullOrEmpty(s)));

			if (coreFault.folderName != null)
				bucket = coreFault.folderName;
			else if (string.IsNullOrEmpty(bucket))
				bucket = "Unknown";

			var faultSummary = new FaultSummary()
			{
				FaultUrl = FaultService.Prefix + "/" + id,
				Reproducable = initialData != null,
				Iteration = coreFault.iteration,
				TimeStamp = System.DateTime.UtcNow,
				BucketName = bucket,
				Source = coreFault.detectionSource,
				Exploitability = coreFault.exploitability ?? "",
				MajorHash = coreFault.majorHash ?? "",
				MinorHash = coreFault.minorHash ?? "",
			};

			var faultDetail = new FaultDetail()
			{
				FaultUrl = faultSummary.FaultUrl,
				Reproducable = faultSummary.Reproducable,
				Iteration = faultSummary.Iteration,
				TimeStamp = faultSummary.TimeStamp,
				BucketName = faultSummary.BucketName,
				Source = faultSummary.Source,
				Exploitability = faultSummary.Exploitability,
				MajorHash = faultSummary.MajorHash,
				MinorHash = faultSummary.MinorHash,

				NodeUrl = NodeService.Prefix + "/" + NodeGuid,
				TargetUrl = "",
				TargetConfigUrl = "",
				PitUrl = "",
				PeachUrl = "",
				Title = coreFault.title,
				Description = coreFault.description,
				Seed = context.config.randomSeed,
				Files = new List<File>(),
			};

			lock (this)
			{
				faults.Add(faultSummary);
				faultDetails.Add(id, faultDetail);

				++FaultCount;
			}
		}
	}
}
