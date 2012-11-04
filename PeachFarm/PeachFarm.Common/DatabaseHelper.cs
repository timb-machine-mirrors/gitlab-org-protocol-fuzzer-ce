using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Driver;
using PeachFarm.Common.Messages;
using System.IO;
using MongoDB.Driver.Builders;

namespace PeachFarm.Common
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

    public static void GetFilesFromDatabase(string connectionString, string destinationPath, string commandLineSearch = "")
    {
      if(Directory.Exists(destinationPath) == false)
      {
        throw new DirectoryNotFoundException(String.Format("Path not found: {0}", destinationPath));
      }

      MongoServer server = MongoServer.Create(connectionString);
      MongoDatabase db = server.GetDatabase("PeachFarm");

      if (db.CollectionExists("runs") == false)
      {
        throw new ApplicationException("Database does not exist.");
      }

      MongoCollection<Run> collection = db.GetCollection<Run>("runs");

      MongoCursor<Run> cursor = null;

      cursor = collection.FindAll();

      foreach (Run run in cursor)
      {
        // Peach 2
        //if ((commandLineSearch.Length == 0) || (run.CommandLine.IndexOf(commandLineSearch) >= 0))

        if ((commandLineSearch.Length == 0) || (run.PitFilePath.IndexOf(commandLineSearch) >= 0))
        {
          foreach (LogFile file in run.LogFiles)
          {
            string filepath = Path.Combine(destinationPath, file.FileName);
            if (Directory.Exists(Path.GetDirectoryName(filepath)) == false)
            {
              Directory.CreateDirectory(Path.GetDirectoryName(filepath));
            }
            var query = Query.EQ("_id", file.FileReference);
            db.GridFS.Download(filepath, query);
          }
        }
      }
    }
  }
}
