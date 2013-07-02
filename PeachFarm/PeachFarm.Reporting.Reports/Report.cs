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
		public static List<Fault> GetJobDetailReport(string jobID, string connectionString)
		{
			Job job = DatabaseHelper.GetJob(jobID, connectionString);
			job.FillNodes(connectionString);
			job.FillFaults(connectionString, true);
			return job.Faults;
		}
	}
}