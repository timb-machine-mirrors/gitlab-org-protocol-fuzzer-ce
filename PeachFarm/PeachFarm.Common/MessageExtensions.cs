using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Xml.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.GridFS;
using MongoDB.Driver.Builders;
using System.Security.Cryptography;

namespace PeachFarm.Common.Messages
{
   
  public partial class StartPeachRequest
  {
    #region pit file support
    /*  
    public static StartPeachRequest CreateStartPeachRequest(string pitFileName)
    {
      StreamReader reader = null;
      string pitFileText;
      try
      {
        reader = new StreamReader(pitFileName);
        pitFileText = reader.ReadToEnd();
      }
      catch
      {
        throw;
      }
      finally
      {
        if(reader != null)
        {
          reader.Close();
        }
      }

      StartPeachRequest request = new StartPeachRequest();
      request.PitXml = pitFileText;

      return request;
    }
    //*/
    #endregion
  }

  public static class ExtensionMethods
  {
    public static List<Run> DatabaseInsert(this List<Run> runs, string connectionString)
    {
      MongoServer server = MongoServer.Create(connectionString);
      MongoDatabase db = server.GetDatabase("PeachFarm");

      MongoCollection<Run> collection = null;

      if (db.CollectionExists("runs"))
      {
        collection = db.GetCollection<Run>("runs");
      }
      else
      {
        db.CreateCollection("runs");
        collection = db.GetCollection<Run>("runs");
        collection.EnsureIndex(new string[] { "ComputerName", "CommandLine", "RunName" });
      }

      foreach (Run run in runs)
      {
        #region Peach 2
        /*
        if (run._id == null)
        {
          foreach (LogFile file in run.LogFiles)
          {
            file.FileReference = file.Upload(db);
          }
        }
        else
        {
          foreach (LogFile file in run.LogFiles)
          {
            if (file.FileReference == null)
            {
              file.FileReference = file.Upload(db);
            }
            else
            {
              string localhash = GetMD5HashFromFile(file.LocalFileName);
              string dbhash = db.GridFS.FindOneById(BsonValue.Create(file.FileReference)).MD5;
              if (localhash.Equals(dbhash) == false)
              {
                db.GridFS.DeleteById(file.FileReference);
                file.FileReference = file.Upload(db);
              }
            }
          }
        }
        //*/
        #endregion

        collection.Save(run);
      }


      server.Disconnect();

      return runs;
    }

    public static List<Fault> DatabaseInsert(this List<Fault> faults, string connectionString)
    {
      MongoServer server = MongoServer.Create(connectionString);
      MongoDatabase db = server.GetDatabase("PeachFarm");

      MongoCollection<Fault> collection = null;

      if (db.CollectionExists("faults"))
      {
        collection = db.GetCollection<Fault>("faults");
      }
      else
      {
        db.CreateCollection("faults");
        collection = db.GetCollection<Fault>("fault");
        collection.EnsureIndex(new string[] { "RunID" });
      }

      foreach (Fault fault in faults)
      {
        collection.Save(fault);
      }


      server.Disconnect();

      return faults;
    }

    public static Run Find(this List<Run> runs, string name)
    {
      var results = (from run in runs where run.TestName == name select run);
      if (results.Count() == 0)
      {
        return null;
      }
      else
      {
        return results.First();
      }
    }

    #region Peach 2
    /*
    private static string GetMD5HashFromFile(string fileName)
    {
      FileStream file = new FileStream(fileName, FileMode.Open);
      MD5 md5 = new MD5CryptoServiceProvider();
      byte[] retVal = md5.ComputeHash(file);
      file.Close();

      StringBuilder sb = new StringBuilder();
      for (int i = 0; i < retVal.Length; i++)
      {
        sb.Append(retVal[i].ToString("x2"));
      }
      return sb.ToString();
    }

    public static BsonObjectId Upload(this LogFile logFile, MongoDatabase db)
    {
      try
      {
        var gridfsInfo = db.GridFS.Upload(logFile.LocalFileName, logFile.FileName);
        return gridfsInfo.Id as BsonObjectId;
      }
      catch (Exception ex)
      {
        Console.WriteLine(String.Format("Could not upload file: {0}\n{1}", logFile.FileName, ex.Message));
        return null;
      }
    }

    public static LogFile Find(this List<LogFile> logFiles, string name)
    {
      var results = (from logFile in logFiles where logFile.FileName == name select logFile);
      if (results.Count() == 0)
      {
        return null;
      }
      else
      {
        return results.First();
      }
    }
    //*/
    #endregion
  }

  public partial class Fault
  {
    public Fault(BsonObjectId runid)
    {
      this.runid = runid;
    }

    [XmlIgnore]
    public BsonObjectId _id { get; set; }

    [XmlAttribute]
    [BsonIgnore]
    public string ID
    {
      get
      {
        if ((_id == null) || (_id == BsonObjectId.Empty))
          return String.Empty;
        else
          return _id.AsString;
      }
    }


    [XmlIgnore]
    public BsonObjectId runid { get; set; }

    [XmlAttribute]
    [BsonIgnore]
    public string RunID
    {
      get
      {
        if ((runid == null) || (runid == BsonObjectId.Empty))
          return String.Empty;
        else
          return _id.AsString;
      }
    }
  }

  public partial class Run
  {
    public Run()
    {
      this.JobID = Guid.NewGuid();
    }

    [XmlIgnore]
    public BsonObjectId _id { get; set; }

    [XmlAttribute]
    [BsonIgnore]
    public string ID
    {
      get 
      {
        if ((_id == null) || (_id == BsonObjectId.Empty))
          return String.Empty;
        else
          return _id.AsString; 
      }
    }

    public List<Fault> GetFaults(string connectionString)
    {
      MongoServer server = MongoServer.Create(connectionString);
      MongoDatabase db = server.GetDatabase("PeachFarm");

      if (db.CollectionExists("faults") == false)
      {
        throw new ApplicationException("Database does not exist.");
      }

      MongoCollection<Fault> faults = db.GetCollection<Fault>("faults");

      var query = Query.EQ("RunID", this._id);
      MongoCursor<Fault> cursor = faults.Find(query);
      return cursor.ToList();
    }
  }

  #region Peach 2
  /*
  public partial class LogFile
  {
    //[BsonIgnore]
    //[XmlElement(Order = 1)]
    //public byte[] Data { get; set; }

    [BsonIgnore]
    [XmlElement(Order = 1)]
    public string LocalFileName { get; set; }

    [XmlIgnore]
    public BsonObjectId FileReference { get; set; }
  }
  //*/
  #endregion
}
