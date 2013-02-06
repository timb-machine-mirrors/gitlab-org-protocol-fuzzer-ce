using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Driver;
using PeachFarm.Common.Messages;
using System.IO;
using MongoDB.Driver.Builders;

namespace PeachFarm.Common.Mongo
{
  public class DatabaseHelper
  {
    public static bool TestConnection(string connectionString)
    {
      bool result = true;
      try
      {
        MongoServer server = MongoServer.Create(connectionString);
        server.Connect();
        server.Disconnect();
      }
      catch
      {
        result = false;
      }
      return result;
    }

    //public static Job GetJob(Guid jobGuid, string connectionString)
    public static Job GetJob(string jobGuid, string connectionString)
    {
      string collectionname = MongoNames.Jobs;
      MongoServer server = MongoServer.Create(connectionString);
      MongoDatabase db = server.GetDatabase(MongoNames.Database);

      MongoCollection<Job> collection;

      if (db.CollectionExists(collectionname))
      {
        collection = db.GetCollection<Job>(collectionname);
      }
      else
      {
        db.CreateCollection(collectionname);
        collection = db.GetCollection<Job>("jobs");
        //collection.CreateIndex(new string[] { "JobID", "TestName", "ComputerName" });
      }


      if (db.CollectionExists(collectionname) == false)
      {
        throw new ApplicationException("Database does not exist.");
      }


      var query = Query.EQ("JobID", jobGuid);
      return collection.FindOne(query);
    }

    public static List<Messages.Heartbeat> GetErrors(string connectionString)
    {
      string collectionName = MongoNames.PeachFarmErrors;
      MongoServer server = MongoServer.Create(connectionString);
      MongoDatabase db = server.GetDatabase(MongoNames.Database);

      MongoCollection<Heartbeat> collection;

      if (db.CollectionExists(collectionName))
      {
        collection = db.GetCollection<Heartbeat>(collectionName);
      }
      else
      {
        db.CreateCollection(collectionName);
        collection = db.GetCollection<Heartbeat>(collectionName);
        //collection.CreateIndex(new string[] { "JobID", "TestName", "ComputerName" });
      }


      if (db.CollectionExists(collectionName) == false)
      {
        throw new ApplicationException("Database does not exist.");
      }

      return collection.FindAll().ToList();
    }
  }

  public static class MongoNames
  {
    public const string Database = "PeachFarm";
    public const string Jobs = "jobs";
    public const string Faults = "faults";
    public const string OutputData = "faultGeneratingData";
    public const string FaultData = "faultInfo";
    public const string PeachFarmErrors = "peachFarmErrors";

  }
}
