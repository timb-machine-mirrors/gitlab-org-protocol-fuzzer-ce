using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PeachFarm.Common.Mongo;

namespace PeachFarm.Loggers
{
	[Peach.Core.Logger("PeachFarmMongo", true)]
	[Peach.Core.Logger("logger.PeachFarm.Mongo")]
	[Peach.Core.Parameter("MongoDbConnectionString", typeof(string), "Connection string to Mongo database")]
	[Peach.Core.Parameter("JobID", typeof(string), "")]
	[Peach.Core.Parameter("NodeName", typeof(string), "")]
	[Peach.Core.Parameter("PitFileName", typeof(string), "")]
	[Peach.Core.Parameter("Path", typeof(string),"")]
	public class MongoLogger : Peach.Core.Loggers.FileLogger
	{
		private Common.Mongo.Job mongoJob = null;
		private Common.Mongo.Node mongoNode = null;

		private static NLog.Logger nlog = NLog.LogManager.GetCurrentClassLogger();

		public MongoLogger(Dictionary<string, Peach.Core.Variant> args)
			:base(args)
		{

			MongoConnectionString = (string)args["MongoDbConnectionString"];
			JobID = (string)args["JobID"];
			NodeName = (string)args["NodeName"];
			PitFileName = (string)args["PitFileName"];
			
		}

		#region Properties
		public string MongoConnectionString
		{
			get;
			private set;
		}

		public string JobID
		{
			get;
			private set;
		}

		public string NodeName { get; private set; }

		public string PitFileName { get; private set; }

		public uint IterationCount { get; private set; }
		#endregion

		#region Logger overrides
		/// <summary>
		/// first fault, going to reproduce
		/// </summary>
		/// <param name="context"></param>
		/// <param name="currentIteration"></param>
		/// <param name="stateModel"></param>
		/// <param name="faultData"></param>
		protected override void Engine_ReproFault(Peach.Core.RunContext context, uint currentIteration, Peach.Core.Dom.StateModel stateModel, Peach.Core.Fault[] faultData)
		{
			base.Engine_ReproFault(context, currentIteration, stateModel, faultData);

			mongoNode.FaultCount++;
			mongoNode.SaveToDatabase(MongoConnectionString);
		}

		/// <summary>
		/// reproduced fault
		/// </summary>
		/// <param name="context"></param>
		/// <param name="currentIteration"></param>
		/// <param name="stateModel"></param>
		/// <param name="faultData"></param>
		protected override void Engine_Fault(Peach.Core.RunContext context, uint currentIteration, Peach.Core.Dom.StateModel stateModel, Peach.Core.Fault[] faultData)
		{
			base.Engine_Fault(context, currentIteration, stateModel, faultData);

			mongoNode.FaultCount++;
			mongoNode.SaveToDatabase(MongoConnectionString);
		}

		protected override void Engine_TestStarting(Peach.Core.RunContext context)
		{
			base.Engine_TestStarting(context);

			System.Console.WriteLine("******** PEACH: START TEST ************");
			mongoJob = DatabaseHelper.GetJob(JobID, MongoConnectionString);
			mongoNode = DatabaseHelper.GetJobNode(NodeName, JobID, MongoConnectionString);
		}

		protected override void Engine_IterationStarting(Peach.Core.RunContext context, uint currentIteration, uint? totalIterations)
		{
			base.Engine_IterationStarting(context, currentIteration, totalIterations);

			mongoNode.IterationCount = currentIteration;
			mongoNode.SaveToDatabase(MongoConnectionString);
		}

		protected override string GetBasePath(Peach.Core.RunContext context)
		{
			return Path;
		}

		protected override System.IO.TextWriter OpenStatusLog()
		{
			return DatabaseHelper.CreateFileGridFS(System.IO.Path.Combine(Path, "status.txt"), MongoConnectionString);
		}

		protected override void SaveFile(Peach.Core.Loggers.FileLogger.Category category, string fullPath, byte[] contents)
		{
			if (category == Category.Reproducing)
				return;

			DatabaseHelper.SaveToGridFS(contents, fullPath, MongoConnectionString);
		}

		protected override void OnFaultSaved(Category category, Peach.Core.Fault fault, string[] dataFiles)
		{
			if (category == Category.Reproducing)
				return;

			// TODO: Make mongo fault record and add to database
			Fault mongoFault = new Fault();
			mongoFault.ControlIteration = fault.controlIteration;
			mongoFault.ControlRecordingIteration = fault.controlRecordingIteration;
			mongoFault.Description = fault.description;
			mongoFault.DetectionSource = fault.detectionSource;
			mongoFault.Exploitability = fault.exploitability;
			mongoFault.FaultType = fault.type.ToString();
			mongoFault.FolderName = fault.folderName;
			mongoFault.IsReproduction = (category == Category.Faults);
			mongoFault.Iteration = fault.iteration;
			mongoFault.JobID = mongoJob.JobID;
			mongoFault.MajorHash = fault.majorHash;
			mongoFault.MinorHash = fault.minorHash;
			mongoFault.NodeName = mongoNode.Name;
			mongoFault.SeedNumber = mongoNode.SeedNumber;
			mongoFault.Stamp = DateTime.Now;
			//mongoFault.TestName = fault.te
			mongoFault.Title = fault.title;

			mongoFault.GeneratedFiles = new List<GeneratedFile>();
			foreach (var dataFile in dataFiles)
			{
				GeneratedFile newfile = new GeneratedFile();
				newfile.Name = dataFile.Substring(Path.Length + 8);
				newfile.GridFsLocation = dataFile;
				mongoFault.GeneratedFiles.Add(newfile);
			}

			mongoFault.SaveToDatabase(MongoConnectionString);

			// folderName is the bucket name used for this fault
			//string bucket = fault.folderName;
		}
		#endregion

		#region private functions
		/*
		private List<Common.Mongo.Fault> GetMongoFaults(Peach.Core.Fault[] peachFaults, Peach.Core.Dom.StateModel stateModel, Peach.Core.RunContext context, bool isreproduction = false)
		{
			List<Common.Mongo.Fault> mongoFaults = new List<Fault>();

			List<Common.Mongo.Action> mongoActions = GetMongoActions(stateModel);

			foreach(Peach.Core.Fault pf in peachFaults)
			{
				Common.Mongo.Fault mongoFault = new Common.Mongo.Fault();

				mongoFault.Stamp = DateTime.Now;
				mongoFault.JobID = JobID;
				mongoFault.NodeName = this.NodeName;

				mongoFault.ControlIteration = pf.controlIteration;
				mongoFault.ControlRecordingIteration = pf.controlRecordingIteration;
				mongoFault.Description = pf.description;
				mongoFault.DetectionSource = pf.detectionSource;
				mongoFault.Exploitability = pf.exploitability;
				mongoFault.FaultType = pf.type.ToString();
				mongoFault.FolderName = pf.folderName;
				mongoFault.Iteration = pf.iteration;
				mongoFault.MajorHash = pf.majorHash;
				mongoFault.MinorHash = pf.minorHash;
				mongoFault.Title = pf.title;
				mongoFault.IsReproduction = isreproduction;

				mongoFault.TestName = context.test.name;
				mongoFault.SeedNumber = context.config.randomSeed;

				if (String.IsNullOrEmpty(pf.folderName) == false)
				{
					mongoFault.Group = pf.folderName;
				}
				else if (String.IsNullOrEmpty(pf.majorHash) && String.IsNullOrEmpty(pf.minorHash) && String.IsNullOrEmpty(pf.exploitability))
				{
					mongoFault.Group = "Unknown";
				}
				else
				{
					mongoFault.Group = string.Format("{0}_{1}_{2}", pf.exploitability, pf.majorHash, pf.minorHash);
				}

				mongoFault.StateModel = mongoActions;
				mongoFault.CollectedData = GetMongoCollectedData(pf);

				mongoFaults.Add(mongoFault);
			}
			return mongoFaults;
		}

		private List<Common.Mongo.Action> GetMongoActions(Peach.Core.Dom.StateModel stateModel)
		{
			List<Common.Mongo.Action> mongoActions = new List<PeachFarm.Common.Mongo.Action>();
			foreach (Peach.Core.Dom.Action action in stateModel.dataActions)
			{
				if (action.dataModel != null)
				{
					PeachFarm.Common.Mongo.Action mongoAction = new PeachFarm.Common.Mongo.Action();
					mongoAction.ActionName = action.name;
					mongoAction.ActionType = action.type.ToString();
					mongoAction.Parameter = 0;
					mongoAction.Data = action.dataModel.Value.Value;
					mongoActions.Add(mongoAction);
				}
				else if (action.parameters.Count > 0)
				{
					int pcnt = 0;
					foreach (Peach.Core.Dom.ActionParameter param in action.parameters)
					{
						pcnt++;
						PeachFarm.Common.Mongo.Action mongoAction = new PeachFarm.Common.Mongo.Action();
						mongoAction.ActionName = action.name;
						mongoAction.ActionType = action.type.ToString();
						mongoAction.Parameter = pcnt;
						mongoAction.Data = param.dataModel.Value.Value;
						mongoActions.Add(mongoAction);
					}
				}
			}
			return mongoActions;
		}

		private List<Common.Mongo.CollectedData> GetMongoCollectedData(Peach.Core.Fault fault)
		{
			List<Common.Mongo.CollectedData> cds = new List<Common.Mongo.CollectedData>(fault.collectedData.Count);

			foreach (var pair in fault.collectedData)
			{
				Common.Mongo.CollectedData cd = new Common.Mongo.CollectedData();
				cd.Key = pair.Key;
				cd.Data = pair.Value;
				cds.Add(cd);
			}

			return cds;
		}
		//*/
		#endregion
	}
}
