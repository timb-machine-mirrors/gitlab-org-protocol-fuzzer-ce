using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using PeachFarm.Common.Mongo;
using PeachFarm.Common;
using PeachFarmMonitor.Configuration;
using PeachFarmMonitor.Reports;

namespace PeachFarmMonitor
{
  public partial class GetJobOutput : BasePage
  {
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
          monitorconfig = (PeachFarmMonitorSection)ConfigurationManager.GetSection("peachfarmmonitor");
          
          Job job = DatabaseHelper.GetJob(jobID, monitorconfig.MongoDb.ConnectionString);
          if (job != null)
          {
            if (String.IsNullOrEmpty(filepath))
            {
              string temppath = Path.GetTempPath();
              string jobName = String.Format("Job_{0}_{1}", job.JobID, job.Pit.FileName);

              FileWriter.DumpFiles(monitorconfig.MongoDb.ConnectionString, temppath, job);
              string zippath = ZipWriter.GetZip(job, temppath);
              ((List<string>)Session["tempfiles"]).Add(zippath);

              Response.AppendHeader("content-disposition", String.Format("attachment; filename={0}.zip", jobName));
              Response.ContentType = "application/zip";
              Response.WriteFile(zippath);
            }
            else
            {
              filepath = filepath.Replace('/', '\\');
              var temppath = GetTempFile();
              DatabaseHelper.DownloadFromGridFS(temppath, filepath, monitorconfig.MongoDb.ConnectionString);
              string filename = Path.GetFileName(filepath);

              Response.AppendHeader("content-disposition", String.Format("attachment; filename={0}", filename));
              Response.ContentType = "text/plain";
              Response.WriteFile(temppath);
            }
          }
        }
      }
    }
  }
}