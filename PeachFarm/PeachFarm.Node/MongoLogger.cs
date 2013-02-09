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
	//[Peach.Core.Parameter("JobID", typeof(Guid), "")]
	[Peach.Core.Parameter("JobID", typeof(string), "")]
	[Peach.Core.Parameter("UserName", typeof(string), "")]
	[Peach.Core.Parameter("PitFileName", typeof(string), "")]
	public class MongoLogger : Peach.Core.Logger
	{
		private Common.Mongo.Job mongoJob = null;
		private static NLog.Logger nlog = NLog.LogManager.GetCurrentClassLogger();

		public MongoLogger(Dictionary<string, Peach.Core.Variant> args)
		{

			MongoConnectionString = (string)args["MongoDbConnectionString"];
			//JobID = Guid.Parse((string)args["JobID"]);
			JobID = (string)args["JobID"];
			UserName = (string)args["UserName"];
			PitFileName = (string)args["PitFileName"];

			System.Net.IPAddress[] ipaddresses = System.Net.Dns.GetHostAddresses(System.Net.Dns.GetHostName());
			IPAddress = (from i in ipaddresses where i.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork select i).First().ToString();


		}

		public string MongoConnectionString
		{
			get;
			private set;
		}

		//public Guid JobID
		public string JobID
		{
			get;
			private set;
		}

		public string UserName { get; private set; }

		public string PitFileName { get; private set; }

		public string IPAddress
		{
			get;
			private set;
		}

		protected override void Engine_Fault(Peach.Core.RunContext context, uint currentIteration, Peach.Core.Dom.StateModel stateModel, Peach.Core.Fault[] faultData)
		{
			Iteration iteration = new Iteration();
			iteration.IterationNumber = currentIteration;
			iteration.JobID = JobID;
			iteration.NodeName = IPAddress;
			iteration.TestName = context.test.name;
			iteration.SeedNumber = context.config.randomSeed;
			iteration.Stamp = DateTime.Now;

			iteration.StateModel = GetMongoActions(stateModel);
			iteration.Faults = GetMongoFaults(faultData);
			iteration.DatabaseInsert(MongoConnectionString);
		}

		protected override void Engine_TestStarting(Peach.Core.RunContext context)
		{
			System.Console.WriteLine("******** PEACH: START TEST ************");
			

			#region MongoDatabase
			mongoJob = DatabaseHelper.GetJob(JobID, MongoConnectionString);
			if (mongoJob == null)
			{
				mongoJob = new Job();
				mongoJob.JobID = JobID;
				mongoJob.UserName = UserName;
				mongoJob.PitFileName = PitFileName;
				mongoJob.StartDate = DateTime.Now;
				mongoJob = mongoJob.DatabaseInsert(MongoConnectionString);
			}
			#endregion
		}

		protected override void Engine_TestError(Peach.Core.RunContext context, Exception e)
		{
			System.Console.WriteLine("******** MONGOLOGGER: TEST ERROR ************");
			System.Console.WriteLine("******** " + e.Message);
			System.Console.WriteLine("*********************************************");
		}

		protected override void Engine_TestFinished(Peach.Core.RunContext context)
		{

		}

		private List<Common.Mongo.Fault> GetMongoFaults(Peach.Core.Fault[] peachFaults)
		{
			List<Common.Mongo.Fault> mongoFaults = new List<Fault>();
			
			foreach(Peach.Core.Fault pf in peachFaults)
			{
				Common.Mongo.Fault mongoFault = new Common.Mongo.Fault();

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
	}
}
