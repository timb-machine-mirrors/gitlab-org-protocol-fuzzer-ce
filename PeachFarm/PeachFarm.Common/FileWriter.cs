using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PeachFarm.Common.Mongo;


namespace PeachFarmMonitor.Common
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
			string jobFolder = String.Empty;
			string nodeFolder = String.Empty;
			string testFolder = String.Empty;
			string faultsFolder = String.Empty;
			string statusFile = String.Empty;
			string faultFolder = String.Empty;
			string iterationFolder = String.Empty;
			string actionFile = String.Empty;
			string collectedDataFile = String.Empty;

			if((job.Nodes == null) || (job.Nodes.Count == 0))
			{
				job.FillNodes(mongoDbConnectionString);
				foreach (Node node in job.Nodes)
				{
					node.FillFaults(mongoDbConnectionString, false);
				}
			}

			System.Console.WriteLine("Processing Job: " + job.JobID);

			jobFolder = Path.Combine(destinationFolder, "Job_" + job.JobID + "_" + job.Pit.FileName);
			CreateDirectory(jobFolder);

			foreach (Node node in job.Nodes)
			{
				nodeFolder = Path.Combine(jobFolder, "Node_" + node.Name);
				CreateDirectory(nodeFolder);

				foreach (Fault fault in node.Faults)
				{
					testFolder = Path.Combine(nodeFolder, String.Format("{0}_{1}_{2}", job.Pit.FileName, fault.TestName, FormatDate(job.StartDate)));
					CreateDirectory(testFolder);

					statusFile = Path.Combine(testFolder, "status.txt");

					if (File.Exists(statusFile) == false)
					{
						StringBuilder statusHeader = new StringBuilder();
						statusHeader.AppendLine("Peach Fuzzing Run");
						statusHeader.AppendLine("=================");
						statusHeader.AppendLine("");
						statusHeader.AppendLine("Date of run: " + job.StartDate.ToString());
						//log.WriteLine("Peach Version: " + context.config.version);
						statusHeader.AppendLine("Peach Version: 3.0.0.0");

						statusHeader.AppendLine("Seed: " + fault.SeedNumber.ToString());

						statusHeader.AppendLine("Command line: " + job.Pit.FileName);
						statusHeader.AppendLine("Pit File: " + job.Pit.FileName);
						statusHeader.AppendLine(". Test starting: " + fault.TestName);
						statusHeader.AppendLine("");
						File.AppendAllText(statusFile, statusHeader.ToString());
					}

					File.AppendAllText(statusFile, String.Format("! Fault detected at iteration {0} : {1}\n", fault.Iteration.ToString(), fault.Stamp.ToString()));

					faultsFolder = Path.Combine(testFolder, "Faults");
					CreateDirectory(faultsFolder);


					#region set faultFolder
					if (fault.FolderName != null)
					{
						faultFolder = System.IO.Path.Combine(faultsFolder, fault.FolderName);
					}
					else if (String.IsNullOrEmpty(fault.MajorHash) && String.IsNullOrEmpty(fault.MinorHash) && String.IsNullOrEmpty(fault.Exploitability))
					{
						faultFolder = System.IO.Path.Combine(faultsFolder, "Unknown");
					}
					else
					{
						faultFolder = System.IO.Path.Combine(faultsFolder,
							string.Format("{0}_{1}_{2}", fault.Exploitability, fault.MajorHash, fault.MinorHash));
					}
					#endregion
					CreateDirectory(faultFolder);

					iterationFolder = Path.Combine(faultFolder, fault.Iteration.ToString());
					CreateDirectory(iterationFolder);

					#region write action files
					int cnt = 0;
					foreach (PeachFarm.Common.Mongo.Action action in fault.StateModel)
					{
						cnt++;
						if (action.Parameter == 0)
						{
							actionFile = System.IO.Path.Combine(iterationFolder, string.Format("action_{0}_{1}_{2}.txt",
											cnt, action.ActionType.ToString(), action.ActionName));

						}
						else
						{
							actionFile = System.IO.Path.Combine(iterationFolder, string.Format("action_{0}-{1}_{2}_{3}.txt",
											cnt, action.Parameter, action.ActionType.ToString(), action.ActionName));
						}

						if (File.Exists(actionFile) == false)
						{
							if (action.Data == null)
							{
								DatabaseHelper.DownloadFromGridFS(actionFile, action.DataPath, mongoDbConnectionString);
							}
							else
							{
								File.WriteAllBytes(actionFile, action.Data);
							}
						}
					}
					#endregion

					#region write collected data files
					foreach (CollectedData cd in fault.CollectedData)
					{
						collectedDataFile = System.IO.Path.Combine(iterationFolder,
							fault.DetectionSource + "_" + cd.Key);

						if (File.Exists(collectedDataFile) == false)
						{
							if (cd.Data == null)
							{
								DatabaseHelper.DownloadFromGridFS(collectedDataFile, cd.DataPath, mongoDbConnectionString);
							}
							else
							{
								File.WriteAllBytes(collectedDataFile, cd.Data);
							}
						}
					}
					#endregion
				}
			}
    }

    private static string FormatDate(DateTime dateTime)
    {
      return String.Format("{0:yyyyMMddhhmmss}", dateTime);
    }

    public static void CreateDirectory(string folder)
    {
      if (Directory.Exists(folder) == false)
        Directory.CreateDirectory(folder);
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
	}
}
