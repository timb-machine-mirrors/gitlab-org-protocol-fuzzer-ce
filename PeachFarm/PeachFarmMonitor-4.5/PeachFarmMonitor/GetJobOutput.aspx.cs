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
using PeachFarmMonitor.Common;
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
        string jobID = Request.QueryString["jobid"];
        string filepath = Request.QueryString["file"];
        
        if (String.IsNullOrEmpty(filepath) == false && String.IsNullOrEmpty(jobID))
        {
          jobID = filepath.Substring(4, 12);
        }

        if (String.IsNullOrEmpty(jobID) == false)
        {
          adminconfig = (AdminSection)ConfigurationManager.GetSection("peachfarm.admin");
          monitorconfig = (PeachFarmMonitorSection)ConfigurationManager.GetSection("peachfarmmonitor");

          Job job = DatabaseHelper.GetJob(jobID, monitorconfig.MongoDb.ConnectionString);
          if (job != null)
          {
            string root = Server.MapPath(".");
            string archiveFolder = Path.Combine(root, "jobArchive");

            if (String.IsNullOrEmpty(filepath))
            {
              FileWriter.DumpFiles(monitorconfig.MongoDb.ConnectionString, archiveFolder, job);
              string zippath = ZipWriter.GetZip(job, archiveFolder);
              var jobName = String.Format("Job_{0}_{1}", job.JobID, job.Pit.FileName);
              Response.AppendHeader("content-disposition", String.Format("attachment; filename={0}.zip", jobName));
              Response.ContentType = "application/zip";
              Response.WriteFile(zippath);
            }
            else
            {
              filepath = Path.Combine(archiveFolder, filepath);
              FileWriter.DumpFiles(monitorconfig.MongoDb.ConnectionString, archiveFolder, job);
              string filename = Path.GetFileName(filepath);
              Response.AppendHeader("content-disposition", String.Format("attachment; filename={0}", filename));
              Response.WriteFile(filepath);
            }
          }
        }
      }
    }
  }
}