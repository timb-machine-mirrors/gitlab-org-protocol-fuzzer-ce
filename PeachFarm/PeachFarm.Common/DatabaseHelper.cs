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

    public static Job GetJob(Guid jobGuid, string connectionString)
    {
      MongoServer server = MongoServer.Create(connectionString);
      MongoDatabase db = server.GetDatabase("PeachFarm");

      MongoCollection<Job> collection;

      if (db.CollectionExists("jobs"))
      {
        collection = db.GetCollection<Job>("jobs");
      }
      else
      {
        db.CreateCollection("jobs");
        collection = db.GetCollection<Job>("jobs");
        collection.CreateIndex(new string[] { "JobID", "TestName", "ComputerName" });
      }


      if (db.CollectionExists("jobs") == false)
      {
        throw new ApplicationException("Database does not exist.");
      }


      var query = Query.EQ("JobID", jobGuid);
      return collection.FindOne(query);
    }

  }


}
