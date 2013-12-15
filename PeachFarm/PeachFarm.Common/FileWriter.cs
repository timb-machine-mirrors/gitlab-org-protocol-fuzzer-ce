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

		public static void DumpFiles(string mongoDbConnectionString, string destinationFolder, string[] ignoreextensions = null)
		{
			List<Job> jobs = DatabaseHelper.GetAllJobs(mongoDbConnectionString);
			foreach (Job job in jobs)
			{
				ProcessJob(job, mongoDbConnectionString, destinationFolder, ignoreextensions);
			}
		}

		public static void DumpFiles(string mongoDbConnectionString, string destinationFolder, string jobID, string[] ignoreextensions = null)
		{
			Job job = DatabaseHelper.GetJob(jobID, mongoDbConnectionString);
			if (job != null)
			{
				ProcessJob(job, mongoDbConnectionString, destinationFolder, ignoreextensions);
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
    /// <param name="ignoreextensions">File extensions to ignore</param>
    /// <returns>Returns zip file path if getZip is true, otherwise empty</returns>
		public static void DumpFiles(string mongoDbConnectionString, string destinationFolder, Job job, string[] ignoreextensions = null)
    {
      ProcessJob(job, mongoDbConnectionString, destinationFolder, ignoreextensions);
    }

		private static void ProcessJob(Job job, string mongoDbConnectionString, string destinationFolder, string[] ignoreextensions = null)
    {
			MongoServer server = new MongoClient(mongoDbConnectionString).GetServer();
			MongoDatabase db = server.GetDatabase(MongoNames.Database);
			string jobfolder = job.JobFolder + "*";
			var query = Query.Matches("filename", new MongoDB.Bson.BsonRegularExpression(jobfolder, "i"));
			var files = db.GridFS.Find(query);
			foreach(var file in files)
			{
				bool ignore = ((ignoreextensions != null) && (ignoreextensions.Contains(Path.GetExtension(file.Name))));
				if(!ignore)
				{
					string localFile = Path.Combine(destinationFolder, file.Name);
					CreateDirectory(Path.GetDirectoryName(localFile));
					try
					{
						db.GridFS.Download(localFile, file.Name);
					}
					catch { }
				}
			}
			server.Disconnect();
		}

		public static void CreateDirectory(string folder)
		{
			if (Directory.Exists(folder) == false)
			{
				Directory.CreateDirectory(folder);
			}
		}
	}
}
