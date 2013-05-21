using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using MongoDB.Driver.Builders;
using PeachFarm.Common.Mongo;
using PeachFarmMonitor.Configuration;
using PeachFarmMonitor.ViewModels;
using Telerik.Web.UI;

namespace PeachFarmMonitor
{
  public partial class JobDetail : System.Web.UI.Page
  {
    #region private variables
    private static PeachFarmMonitorSection monitorconfig = null;
    private string jobid;
    private bool jobfound;

    private static string[] dataFields = new string[]
		{
		    "CollectedData.Data",
		    "StateModel.Data"
		};

    private static string[] collectionFields = new string[]
    {
      "CollectedData",
      "StateModel"
    };
    #endregion

    #region Ctor
    public JobDetail()
    {
      this.InitComplete += JobDetail_InitComplete;
    }
    #endregion

    #region Properties
    public JobViewModel Job { get; private set; }
    #endregion

    #region Page event handlers
    void JobDetail_InitComplete(object sender, EventArgs e)
    {
      jobid = Request.QueryString["jobid"];
      PeachFarm.Common.Mongo.Job job = null;
      if (String.IsNullOrEmpty(jobid) == false)
      {
        monitorconfig = (PeachFarmMonitorSection)ConfigurationManager.GetSection("peachfarmmonitor");
        //Job = new JobViewModel(Reports.Report.GetJobDetailReport(jobid, monitorconfig.MongoDb.ConnectionString, 0, 0));
        //jobfound = (Job != null);
        job = PeachFarm.Common.Mongo.DatabaseHelper.GetJob(jobid, monitorconfig.MongoDb.ConnectionString);
        jobfound = job != null;
      }
      else
      {
        lblJobID.Text = "No Job ID supplied";
        return;
      }

      if (jobfound)
      {
        Job = new JobViewModel(job);

        faultBucketsGrid.NeedDataSource += faultBucketsGrid_NeedDataSource;
        faultsGrid.NeedDataSource += faultsGrid_NeedDataSource;
        faultsGrid.DetailTableDataBind += faultsGrid_DetailTableDataBind;

        lblJobID.Text = job.Pit.FileName + " - " + job.JobID;
        downloadInputLink.NavigateUrl = "GetJobOutput.aspx?file=" + job.ZipFile;
        downloadOutputLink.NavigateUrl = "GetJobOutput.aspx?jobid=" + jobid;
        viewReportLink.NavigateUrl = "ReportViewer.aspx?jobid=" + jobid;
        Page.Title = "Job Detail: " + job.Pit.FileName;
      }
      else
      {
        lblJobID.Text = String.Format("Job {0} not found.", jobid);
        return;
      }


    }

    #endregion

    #region faultBucketsGrid event handlers

    void faultBucketsGrid_NeedDataSource(object sender, GridNeedDataSourceEventArgs e)
    {
      if (e.IsFromDetailTable == false)
      {
        var faultBuckets = JobDetailData.GetFaultBuckets(jobid);

        faultBucketsGrid.DataSource = faultBuckets;
        //faultBucketsGrid.MasterTableView.VirtualItemCount = faultBuckets.Count;
      }
			faultBucketsGrid.NeedDataSource -= faultBucketsGrid_NeedDataSource;

    }
    
    protected void faultBucketsGrid_ItemCommand(object sender, GridCommandEventArgs e)
    {
      switch (e.CommandName)
      {
        case "FaultBucketSelect":
          int pagesize = faultsGrid.MasterTableView.PageSize;
          int pageindex = 0;

          e.Item.Selected = true;
          var group = ((GridDataItem)e.Item)["FolderName"].Text;
          ViewState["currentGroup"] = group;

          var faults = JobDetailData.GetFaults(jobid, group, pagesize, pageindex);

          faultsGrid.DataSource = faults;
          faultsGrid.MasterTableView.VirtualItemCount = Convert.ToInt32(((GridDataItem)e.Item)["FaultCount"].Text);
          faultsGrid.DataBind();

          break;
      }
    }
    #endregion

    #region faultsGrid event handlers
    void faultsGrid_NeedDataSource(object sender, GridNeedDataSourceEventArgs e)
    {
      string currentGroup = ViewState["currentGroup"] as string;
      if ((e.IsFromDetailTable == false) && (String.IsNullOrEmpty(currentGroup) == false))
      {
        int pagesize = faultsGrid.MasterTableView.PageSize;
        int pageindex = faultsGrid.MasterTableView.CurrentPageIndex;
        var faults = JobDetailData.GetFaults(jobid, currentGroup, pagesize, pageindex);
        faultsGrid.DataSource = faults;
        //faultsGrid.DataBind();

        //if (faults.Count < pagesize)
        //{
        //  int totalpages = pageindex + 1;
        //  int totalrecords = ((totalpages - 1) * pagesize) + faults.Count;
        //  faultsGrid.MasterTableView.VirtualItemCount = totalrecords;
        //}
      }
			faultsGrid.NeedDataSource -= faultsGrid_NeedDataSource;
		}
    
    protected void faultsGrid_DetailTableDataBind(object sender, Telerik.Web.UI.GridDetailTableDataBindEventArgs e)
    {
      GridDataItem parent = e.DetailTableView.ParentItem;
      if (parent != null)
      {
        switch (e.DetailTableView.DataMember)
        {
          case "Description":
            {
              List<string> description = new List<string>();
              description.Add(((FaultViewModel)parent.DataItem).Description);
              e.DetailTableView.DataSource = description;
            }
            break;
          case "GeneratedFiles":
            {
              e.DetailTableView.DataSource = ((FaultViewModel)parent.DataItem).GeneratedFiles;
            }
            break;
        }
      }
			faultsGrid.DetailTableDataBind -= faultsGrid_DetailTableDataBind;
    }
    #endregion

    #region private functions
    private List<string> reverseStringFormat(string template, string str)
    {
      string pattern = "^" + Regex.Replace(template, @"\{[0-9]+\}", "(.*?)") + "$";

      Regex r = new Regex(pattern);
      Match m = r.Match(str);

      List<string> ret = new List<string>();

      for (int i = 1; i < m.Groups.Count; i++)
      {
        ret.Add(m.Groups[i].Value);
      }

      return ret;
    }
    #endregion

  }

  public class JobDetailData
  {
    private static PeachFarmMonitorSection monitorconfig = (PeachFarmMonitorSection)ConfigurationManager.GetSection("peachfarmmonitor");

    public static List<FaultBucketViewModel> GetFaultBuckets(string jobID)
    {
      var collection = DatabaseHelper.GetCollection<Fault>(MongoNames.Faults, monitorconfig.MongoDb.ConnectionString);
      
      var buckets = collection.Distinct("FolderName", Query.EQ("JobID", jobID));
      List<FaultBucketViewModel> faultBuckets = new List<FaultBucketViewModel>();
      foreach (var bucket in buckets)
      {
        Fault faultBucket = new Fault();
        faultBucket.FolderName = bucket.AsString;
        FaultBucketViewModel fbvm = new FaultBucketViewModel(faultBucket);
				fbvm.FaultCount = collection.Distinct("_id", Query.And(Query.EQ("JobID", jobID), Query.EQ("FolderName", faultBucket.FolderName))).Count();
        faultBuckets.Add(fbvm);
      }
      collection.Database.Server.Disconnect();

      return faultBuckets;
    }

    public static List<FaultViewModel> GetFaults(string jobID, string faultBucketName, int pageSize, int pageIndex)
    {
      List<FaultViewModel> vms = new List<FaultViewModel>();
      if (String.IsNullOrEmpty(faultBucketName) == false)
      {
        var collection = DatabaseHelper.GetCollection<Fault>(MongoNames.Faults, monitorconfig.MongoDb.ConnectionString);
        var query = Query.And(Query.EQ("JobID", jobID), Query.EQ("FolderName", faultBucketName));
        List<Fault> faults = null;
        if (pageSize == 0)
        {
          faults = collection.Find(query).ToList();
        }
        else
        {
          int skip = pageIndex * pageSize;
          faults = collection.Find(query).SetSkip(skip).SetLimit(pageSize).ToList();
        }

        foreach (var fault in faults)
        {
          vms.Add(new FaultViewModel(fault));
        }
      }
      return vms;
    }
  }
}