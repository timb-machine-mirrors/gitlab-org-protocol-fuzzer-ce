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
	[Peach.Core.Parameter("Target", typeof(string), "")]
	[Peach.Core.Parameter("NodeName", typeof(string), "")]
	[Peach.Core.Parameter("PitFileName", typeof(string), "")]
	[Peach.Core.Parameter("Path", typeof(string),"")]
	public class MongoLogger : Peach.Core.Loggers.FileLogger
	{
		private Common.Mongo.Job mongoJob = null;
		private Common.Mongo.Node mongoNode = null;

		public MongoLogger(Dictionary<string, Peach.Core.Variant> args)
			:base(args)
		{

			MongoConnectionString = (string)args["MongoDbConnectionString"];
			JobID = (string)args["JobID"];
			NodeName = (string)args["NodeName"];
			PitFileName = (string)args["PitFileName"];
			Target = (string)args["Target"];
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

		public string Target { get; private set; }
		#endregion

		#region Logger overrides

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

			fullPath = fullPath.Replace('/', '\\');
			DatabaseHelper.SaveToGridFS(contents, fullPath, MongoConnectionString);
		}

		protected override void OnFaultSaved(Category category, Peach.Core.Fault fault, string[] dataFiles)
		{
			if (category == Category.Reproducing)
				return;

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
			mongoFault.Title = fault.title;

			mongoFault.GeneratedFiles = new List<GeneratedFile>();
			foreach (var dataFile in dataFiles)
			{
				GeneratedFile newfile = new GeneratedFile();
				newfile.GridFsLocation = dataFile.Replace('\\','/');
				newfile.Name = newfile.GridFsLocation.Substring(Path.Length + 8);
				mongoFault.GeneratedFiles.Add(newfile);
			}

			mongoFault.SaveToDatabase(MongoConnectionString);

			mongoNode.FaultCount++;
			mongoNode.SaveToDatabase(MongoConnectionString);

		}
		#endregion
	}
}
