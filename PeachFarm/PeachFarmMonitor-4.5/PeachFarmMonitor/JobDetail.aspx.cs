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
    private static PeachFarmMonitorSection monitorconfig = null;
    private string jobid;
    private bool jobfound;

    public JobDetail()
    {
      this.InitComplete += JobDetail_InitComplete;
    }

    public JobViewModel Job { get; private set; }

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

        faultsGrid.NeedDataSource += iterationsGrid_NeedDataSource;
        faultsGrid.DetailTableDataBind += faultsGrid_DetailTableDataBind;
        faultsGrid.ItemDataBound += faultsGrid_ItemDataBound;

        lblJobID.Text = job.Pit.FileName + " - " + job.JobID;
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

    void iterationsGrid_NeedDataSource(object sender, GridNeedDataSourceEventArgs e)
    {
      if (e.IsFromDetailTable == false)
      {
        #region pagination code
        //*
        int pagesize = faultsGrid.MasterTableView.PageSize;
        int pageindex = faultsGrid.MasterTableView.CurrentPageIndex;

        var collection = DatabaseHelper.GetCollection<Fault>(MongoNames.Faults, monitorconfig.MongoDb.ConnectionString);

        var buckets = collection.Distinct("Group", Query.EQ("JobID", Job.JobID));
        foreach (var bucket in buckets)
        {
          Fault faultBucket = new Fault();
          faultBucket.Group = bucket.AsString;
          Job.FaultBuckets.Add(new FaultBucketViewModel(faultBucket));
        }
        

        //if (Job.Faults.Count < pagesize)
        //{
        //  int totalpages = pageindex + 1;
        //  int totalrecords = ((totalpages - 1) * pagesize) + Job.Faults.Count;
        //  faultsGrid.MasterTableView.VirtualItemCount = totalrecords;
        //}
        //*/
        #endregion
        
        faultsGrid.DataSource = Job.FaultBuckets;
      }
    }

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

    protected void faultsGrid_DetailTableDataBind(object sender, Telerik.Web.UI.GridDetailTableDataBindEventArgs e)
    {
      GridDataItem parent = e.DetailTableView.ParentItem;
      if (parent != null)
      {
        switch (e.DetailTableView.DataMember)
        {
          case "Faults":
            {
              /*
              uint iterationnumber = (uint)parent.GetDataKeyValue("IterationNumber");
              string nodename = (string)parent.GetDataKeyValue("NodeName");
              IterationViewModel ivm = null;
              if (Job == null)
              {
                PeachFarm.Common.Mongo.Iteration iteration = new PeachFarm.Common.Mongo.Iteration();
                iteration.IterationNumber = iterationnumber;
                iteration.NodeName = nodename;
                ivm = new IterationViewModel(PeachFarm.Common.Mongo.DatabaseHelper.FindIteration(iteration, monitorconfig.MongoDb.ConnectionString));
              }
              else
              {
                ivm = (from i in Job.Iterations where i.IterationNumber == iterationnumber && i.NodeName == nodename select i).First();
              }
              e.DetailTableView.DataSource = ivm.Faults;
              //*/
              var collection = DatabaseHelper.GetCollection<Fault>(MongoNames.Faults, monitorconfig.MongoDb.ConnectionString);
              var query = Query.And(Query.EQ("JobID", Job.JobID), Query.EQ("Group", parent["Group"].Text));
              var faults = collection.Find(query).SetFields(Fields.Exclude(collectionFields));
              collection.Database.Server.Disconnect();
              List<FaultViewModel> vms = new List<FaultViewModel>();
              foreach (var fault in faults)
              {
                vms.Add(new FaultViewModel(fault));
              }
              e.DetailTableView.DataSource = vms;
            }
            break;
          case "StateModel":
            {
              var collection = DatabaseHelper.GetCollection<Fault>(MongoNames.Faults, monitorconfig.MongoDb.ConnectionString);
              var query = Query.EQ("_id", new MongoDB.Bson.BsonObjectId(parent["ID"].Text));
              var fault = collection.Find(query).SetFields(Fields.Exclude(dataFields)).First();

              List<ActionViewModel> vms = new List<ActionViewModel>();
              foreach (var action in fault.StateModel)
              {
                vms.Add(new ActionViewModel(action));
              }
              e.DetailTableView.DataSource = vms;
            }
            break;
          case "CollectedData":
            {
              var collection = DatabaseHelper.GetCollection<Fault>(MongoNames.Faults, monitorconfig.MongoDb.ConnectionString);
              var query = Query.EQ("_id", new MongoDB.Bson.BsonObjectId(parent["ID"].Text));
              var fault = collection.Find(query).SetFields(Fields.Exclude(dataFields)).First();

              List<CollectedDataViewModel> vms = new List<CollectedDataViewModel>();
              foreach (var cd in fault.CollectedData)
              {
                vms.Add(new CollectedDataViewModel(cd));
              }

              e.DetailTableView.DataSource = vms;
            }
            break;
        }
      }
    }

    protected void faultsGrid_ItemDataBound(object sender, Telerik.Web.UI.GridItemEventArgs e)
    {
      GridDataItem item = e.Item as GridDataItem;

      if ((item != null) && (item.DataItem != null) && (item.DetailTemplateItemDataCell != null))
      {
        if (item.DataItem is FaultViewModel)
        {
          FaultViewModel fault = item.DataItem as FaultViewModel;
          var panelbar = item.DetailTemplateItemDataCell.FindControl("descriptionPanel") as RadPanelBar;
          var label = panelbar.Items[0].FindControl("descriptionLabel") as TextBox;
          label.Text = fault.Description;
        }
      }
    }


  }
}