using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using PeachFarm.Common.Mongo;
using PeachFarm.Reporting;

namespace PeachFarm.Reporting.Reports
{
  public class ReportData
  {
    public static List<Job> GetJobDetailReport(string jobID, string connectionString)
    {
      List<Job> jobs = new List<Job>();
      Job job = DatabaseHelper.GetJob(jobID, connectionString);
      job.FillNodes(connectionString);
      job.FillFaults(connectionString, true);
      jobs.Add(job);
      return jobs;
    }
  }
}