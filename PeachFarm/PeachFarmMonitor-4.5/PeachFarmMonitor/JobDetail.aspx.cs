using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
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
        iterationsGrid.NeedDataSource += iterationsGrid_NeedDataSource;
        iterationsGrid.DetailTableDataBind += iterationsGrid_DetailTableDataBind;
        iterationsGrid.ItemDataBound += iterationsGrid_ItemDataBound;

        lblJobID.Text = job.PitFileName + " - " + job.JobID;
        downloadOutputLink.NavigateUrl = "GetJobOutput.aspx?jobid=" + jobid;
        viewReportLink.NavigateUrl = "ReportViewer.aspx?jobid=" + jobid;
        Page.Title = "Job Detail: " + job.PitFileName;
      }
      else
      {
        lblJobID.Text = String.Format("Job {0} not found.", jobid);
        return;
      }


    }

    void iterationsGrid_NeedDataSource(object sender, GridNeedDataSourceEventArgs e)
    {
      if (e.IsFromDetailTable == false)
      {
        #region pagination code
        //*
        int pagesize = iterationsGrid.MasterTableView.PageSize;
        int pageindex = iterationsGrid.MasterTableView.CurrentPageIndex;

        Job = new JobViewModel(Reports.Report.GetJobDetailReport(jobid, monitorconfig.MongoDb.ConnectionString, pageindex, pagesize));

        if (Job.Iterations.Count < pagesize)
        {
          int totalpages = pageindex + 1;
          int totalrecords = ((totalpages - 1) * pagesize) + Job.Iterations.Count;
          iterationsGrid.MasterTableView.VirtualItemCount = totalrecords;
        }
        //*/
        #endregion
        
        iterationsGrid.DataSource = Job.Iterations;
      }
    }

    protected void iterationsGrid_DetailTableDataBind(object sender, Telerik.Web.UI.GridDetailTableDataBindEventArgs e)
    {
      GridDataItem parent = e.DetailTableView.ParentItem;
      if (parent != null)
      {
        switch (e.DetailTableView.DataMember)
        {
          case "Faults":
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
            e.DetailTableView.DataSource = ((IterationViewModel)parent.DataItem).Faults;
            break;
          case "StateModel":
            e.DetailTableView.DataSource = ((FaultViewModel)parent.DataItem).StateModel;
            break;
          case "CollectedData":
            e.DetailTableView.DataSource = ((FaultViewModel)parent.DataItem).CollectedData;
            break;
        }
      }
    }

    protected void iterationsGrid_ItemDataBound(object sender, Telerik.Web.UI.GridItemEventArgs e)
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