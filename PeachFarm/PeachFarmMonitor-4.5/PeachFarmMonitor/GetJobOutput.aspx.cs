using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using PeachFarm.Admin.Configuration;
using PeachFarm.Common.Mongo;
using PeachFarmMonitor.Configuration;
using PeachFarmMonitor.Reports;

namespace PeachFarmMonitor
{
  public partial class GetJobOutput : System.Web.UI.Page
  {
    private static AdminSection adminconfig = null;
    private static PeachFarmMonitorSection monitorconfig = null;

    protected void Page_Load(object sender, EventArgs e)
    {
      if (!IsPostBack)
      {
        string jobID;
        if (String.IsNullOrEmpty(jobID = Request.QueryString["jobid"]) == false)
        {
          adminconfig = (AdminSection)ConfigurationManager.GetSection("peachfarm.admin");
          monitorconfig = (PeachFarmMonitorSection)ConfigurationManager.GetSection("peachfarmmonitor");

          Job job = DatabaseHelper.GetJob(jobID, monitorconfig.MongoDb.ConnectionString);
          if (job != null)
          {
            string root = Server.MapPath(".");
            string archiveFolder = Path.Combine(root, "jobArchive");
            //FileWriter.CreateDirectory(archiveFolder);
            string zippath = FileWriter.DumpFiles(monitorconfig.MongoDb.ConnectionString, archiveFolder, job, true);
            var jobName = String.Format("Job_{0}_{1}", job.JobID, job.PitFileName);
            Response.AppendHeader( "content-disposition", "attachment; filename=" + jobName + ".zip");
            Response.ContentType = "application/zip";
            Response.WriteFile(zippath);
          }
        }
      }
    }
  }
}