using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PeachFarm.Common.Mongo;
using MongoDB.Driver;
using MongoDB.Driver.Builders;


namespace PeachFarm.Common
{
  public class FileWriter
  {

		public static void DumpFiles(string mongoDbConnectionString, string destinationFolder)
		{
			List<Job> jobs = DatabaseHelper.GetAllJobs(mongoDbConnectionString);
			foreach (Job job in jobs)
			{
				ProcessJob(job, mongoDbConnectionString, destinationFolder);
			}
		}

		public static void DumpFiles(string mongoDbConnectionString, string destinationFolder, string jobID)
		{
			Job job = DatabaseHelper.GetJob(jobID, mongoDbConnectionString);
			if (job != null)
			{
				ProcessJob(job, mongoDbConnectionString, destinationFolder);
			}
			else
			{
				throw new ApplicationException("Peach Farm database does not contain this Job ID: " + jobID);
			}
		}

    /// <summary>
    /// Creates job output directory structure and files, optionally creates zip archive
    /// </summary>
    /// <param name="mongoDbConnectionString">Connection to MongoDB</param>
    /// <param name="destinationFolder">Where the jobs get stored</param>
    /// <param name="job">The job to write</param>
    /// <param name="getZip">Create zip file</param>
    /// <returns>Returns zip file path if getZip is true, otherwise empty</returns>
    public static void DumpFiles(string mongoDbConnectionString, string destinationFolder, Job job) /*, bool getZip = false */
    {
      ProcessJob(job, mongoDbConnectionString, destinationFolder);

			//if(getZip)
			//{
			//  return GetZip(job, destinationFolder);
			//}

      //return String.Empty;
    }

		private static void ProcessJob(Job job, string mongoDbConnectionString, string destinationFolder)
    {
			MongoServer server = new MongoClient(mongoDbConnectionString).GetServer();
			MongoDatabase db = server.GetDatabase(MongoNames.Database);
			string jobfolder = String.Format("Job_{0}_{1}*", job.JobID, job.Pit.FileName);
			var query = Query.Matches("filename", new MongoDB.Bson.BsonRegularExpression(jobfolder, "i"));
			var files = db.GridFS.Find(query);
			foreach(var file in files)
			{
				string localFile = Path.Combine(destinationFolder, file.Name);
				CreateDirectory(Path.GetDirectoryName(localFile));
				db.GridFS.Download(localFile, file.Name);
			}
			server.Disconnect();
		}

		

		#region GetZip
		/*
		/// <summary>
		/// Only compatible with .NET 4.5
		/// </summary>
		/// <param name="job"></param>
		/// <param name="sourceFolder"></param>
		/// <returns></returns>
		private static string GetZip(Job job, string sourceFolder)
    {
      var jobName = String.Format("Job_{0}_{1}", job.JobID, job.PitFileName);
      var jobFolder = Path.Combine(sourceFolder, jobName);

      string[] files = Directory.GetFiles(jobFolder, "*", SearchOption.AllDirectories);

      var tempfile = Path.GetTempFileName() + ".zip";
      using (FileStream zipToOpen = new FileStream(tempfile, FileMode.Create))
      {
        using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Create))
        {
          foreach (string file in files)
          {
            var zipfile = file.Substring(sourceFolder.Length + 1);
            ZipArchiveEntry entry = archive.CreateEntry(zipfile);

            using (StreamReader reader = new StreamReader(File.OpenRead(file)))
            {
              using (StreamWriter writer = new StreamWriter(entry.Open()))
              {
                writer.Write(reader.ReadToEnd());
              }
            }
          }
        }
      }
      return tempfile;
    }
		//*/
		#endregion

		public static void CreateDirectory(string folder)
		{
			if (Directory.Exists(folder) == false)
			{
				Directory.CreateDirectory(folder);
			}
		}
	}
}
