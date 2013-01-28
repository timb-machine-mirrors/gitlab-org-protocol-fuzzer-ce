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
      try
      {
        List<Common.Mongo.Fault> mongoFaults = new List<Common.Mongo.Fault>();

        System.Console.WriteLine("******** Peach: Engine_Fault ************");


        foreach (Peach.Core.Fault peachFault in faultData)
        {
          #region MongoDB
          PeachFarm.Common.Mongo.Fault mongoFault = GetMongoFault(peachFault, context);
          mongoFault = mongoFault.DatabaseInsert(MongoConnectionString);


          List<FaultData> mongoFaultData = GetMongoFaultData(peachFault, mongoFault);
          mongoFaultData.DatabaseInsert(MongoConnectionString);

          System.Console.WriteLine("******** MONGO: WRITING FAULT ************");

          #endregion
        }

      }
      catch(Exception ex)
      {
        nlog.FatalException("Mongo failed to write", ex);
        //System.Console.WriteLine("******** MONGO: FAILED FAULT ************");
      }
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

    private Common.Mongo.Fault GetMongoFault(Peach.Core.Fault fault, Peach.Core.RunContext context)
    {
      Common.Mongo.Fault mongoFault = new Common.Mongo.Fault();

      mongoFault.ControlIteration = fault.controlIteration;
      mongoFault.ControlRecordingIteration = fault.controlRecordingIteration;
      mongoFault.Description = fault.description;
      mongoFault.DetectionSource = fault.detectionSource;
      mongoFault.Exploitability = fault.exploitability;
      mongoFault.FaultType = fault.type.ToString();
      mongoFault.FolderName = fault.folderName;
      mongoFault.Iteration = fault.iteration;
      mongoFault.MajorHash = fault.majorHash;
      mongoFault.MinorHash = fault.minorHash;
      mongoFault.Title = fault.title;
      mongoFault.JobID = JobID;
      mongoFault.NodeName = IPAddress;
      mongoFault.TestName = context.test.name;

            
      return mongoFault;
    }

    private List<Common.Mongo.FaultData> GetMongoFaultData(Peach.Core.Fault fault, PeachFarm.Common.Mongo.Fault mongoFault)
    {
      List<Common.Mongo.FaultData> mongoFaultData = new List<Common.Mongo.FaultData>(fault.collectedData.Count);

      foreach (var pair in fault.collectedData)
      {
        mongoFaultData.Add(new Common.Mongo.FaultData(mongoFault.JobID, mongoFault._id, pair.Key, pair.Value));
      }

      return mongoFaultData;
    }

  }
}
