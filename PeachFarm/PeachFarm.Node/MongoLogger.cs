using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PeachFarm.Common.Mongo;
using PeachFarm.Common.Messages;

namespace PeachFarm.Loggers
{
//	[Peach.Core.Logger("PeachFarm", true)]
//	[Peach.Core.Logger("logger.PeachFarm")]
	[Peach.Core.Parameter("MongoDbConnectionString", typeof(string), "Connection string to Mongo database")]
	[Peach.Core.Parameter("JobID", typeof(string), "")]
	[Peach.Core.Parameter("NodeName", typeof(string), "")]
	[Peach.Core.Parameter("PitFileName", typeof(string), "")]
	[Peach.Core.Parameter("Path", typeof(string), "Log folder")]
	[Peach.Core.Parameter("RabbitHost", typeof(string), "RabbitHost")]
	[Peach.Core.Parameter("RabbitPort", typeof(int), " RabbitPort")]
	[Peach.Core.Parameter("RabbitUser", typeof(string), "RabbitUser")]
	[Peach.Core.Parameter("RabbitPassword", typeof(string), "RabbitPassword")]
	[Peach.Core.Parameter("RabbitUseSSL", typeof(bool), "RabbitUseSSL")]
	public class PeachFarmLogger : Peach.Core.Loggers.FileLogger
	{
		private Common.Mongo.Job mongoJob = null;
		private Common.Mongo.Node mongoNode = null;

		private const int faultmax = 1;

		public PeachFarmLogger(Dictionary<string, Peach.Core.Variant> args)
			:base(args)
		{

			MongoConnectionString = (string)args["MongoDbConnectionString"];
			JobID = (string)args["JobID"];
			NodeName = (string)args["NodeName"];
			PitFileName = (string)args["PitFileName"];

			FilePath = (string)args["Path"];
			RabbitHost = (string)args["RabbitHost"];
			RabbitPort = (int)args["RabbitPort"];
			RabbitUser = (string)args["RabbitUser"];
			RabbitPassword = (string)args["RabbitPassword"];
			RabbitUseSSL = (bool)args["RabbitUseSSL"];
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


		public string FilePath { get; private set; }
		public string RabbitHost { get; private set; }
		public int RabbitPort { get; private set; }
		public string RabbitUser { get; private set; }
		public bool RabbitUseSSL { get; private set; }
		public string RabbitPassword { get; private set; }
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

			if (currentIteration > mongoNode.IterationCount)
			{
				mongoNode.IterationCount = currentIteration;
				mongoNode.SaveToDatabase(MongoConnectionString);
			}
		}

		protected override string GetBasePath(Peach.Core.RunContext context)
		{
			return Path;
		}

		protected override System.IO.TextWriter OpenStatusLog()
		{
			return DatabaseHelper.CreateFileGridFS(System.IO.Path.Combine(Path, "status.txt"), MongoConnectionString);
		}

		protected override void Engine_TestFinished(Peach.Core.RunContext context)
		{
			base.Engine_TestFinished(context);

		}

		protected override void SaveFile(Peach.Core.Loggers.FileLogger.Category category, string fullPath, byte[] contents)
		{
			if (category == Category.Reproducing)
				return;

			fullPath = fullPath.Replace('/', '\\');
			DatabaseHelper.SaveToGridFS(contents, fullPath, MongoConnectionString);
		}

		JobProgressNotification notification = null;

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
				newfile.Name = newfile.GridFsLocation.Substring(Path.Length);
				mongoFault.GeneratedFiles.Add(newfile);
			}

			var mongoid = mongoFault.SaveToDatabase(MongoConnectionString);

			mongoNode.FaultCount++;
			mongoNode.SaveToDatabase(MongoConnectionString);

			if (notification == null)
			{
				notification = new JobProgressNotification(JobID);
			}

			foreach (var state in fault.states)
			{
				if (HasAny(state.actions))
				{
					foreach (var action in state.actions)
					{
						if (HasAny(action.models))
						{
							foreach (var model in action.models)
							{
								if (HasAny(model.mutations))
								{
									foreach (var mutation in model.mutations)
									{
										notification.FaultMetrics.Add(new FaultMetric()
										{
											Iteration = fault.iteration,
											Bucket = fault.folderName,
											State = state.name,
											Action = action.name,
											DataSet = model.dataSet,
											DataElement = mutation.element,
											DataModel = model.name,
											Parameter = model.parameter,
											Mutator = mutation.mutator,
											MongoID = mongoid
										});
									}
								}
								else
								{	// model with no mutations
								//  notification.FaultMetrics.Add(new FaultMetric()
								//  {
								//    Iteration = fault.iteration,
								//    Bucket = fault.folderName,
								//    State = state.name,
								//    Action = action.name,
								//    DataSet = model.dataSet,
								//    DataModel = model.name,
								//    Parameter = model.parameter,
								//    MongoID = mongoid
								//  });
								}
							}
						}
						else
						{	//action with no models
							//notification.FaultMetrics.Add(new FaultMetric()
							//{
							//  Iteration = fault.iteration,
							//  Bucket = fault.folderName,
							//  State = state.name,
							//  Action = action.name,
							//  MongoID = mongoid
							//});
						}
					}
				}
				else
				{	//state with no actions
					//notification.FaultMetrics.Add(new FaultMetric()
					//{
					//  Iteration = fault.iteration,
					//  Bucket = fault.folderName,
					//  State = state.name,
					//  MongoID = mongoid
					//});
				}
			}

			if (notification.FaultMetrics.Count >= faultmax)
			{
				PeachFarm.Common.RabbitMqHelper rabbit = new Common.RabbitMqHelper(RabbitHost, RabbitPort, RabbitUser, RabbitPassword, RabbitUseSSL);
				rabbit.PublishToQueue(PeachFarm.Common.QueueNames.QUEUE_REPORTGENERATOR, notification.Serialize(), Actions.NotifyJobProgress);
				rabbit.CloseConnection();
				rabbit = null;
				notification = null;
			}
		}
		#endregion

		private bool HasAny<T>(IEnumerable<T> collection)
		{
			return collection != null && collection.Any();
		}
	}

}
