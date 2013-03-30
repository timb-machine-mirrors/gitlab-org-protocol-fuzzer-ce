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

    void JobDetail_InitComplete(object sender, EventArgs e)
    {
      jobid = Request.QueryString["jobid"];
      
      if (String.IsNullOrEmpty(jobid) == false)
      {
        monitorconfig = (PeachFarmMonitorSection)ConfigurationManager.GetSection("peachfarmmonitor");
        jobfound = (PeachFarm.Common.Mongo.DatabaseHelper.GetJob(jobid, monitorconfig.MongoDb.ConnectionString) != null);
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

        lblJobID.Text = jobid;
        downloadOutputLink.NavigateUrl = "GetJobOutput.aspx?jobid=" + jobid;
        viewReportLink.NavigateUrl = "ReportViewer.aspx?jobid=" + jobid;
        Page.Title = "Job Detail: " + jobid;
      }
      else
      {
        lblJobID.Text = String.Format("Job {0} not found.", jobid);
        return;
      }


    }

    void iterationsGrid_NeedDataSource(object sender, GridNeedDataSourceEventArgs e)
    {
      int pagesize = iterationsGrid.MasterTableView.PageSize;
      int pageindex = iterationsGrid.MasterTableView.CurrentPageIndex;

      Job = new JobViewModel(Reports.Report.GetJobDetailReport(jobid, monitorconfig.MongoDb.ConnectionString, pageindex, pagesize));

      if (Job.Iterations.Count < pagesize)
      {
        int totalpages = pageindex + 1;
        int totalrecords = ((totalpages - 1) * pagesize) + Job.Iterations.Count;
        iterationsGrid.MasterTableView.VirtualItemCount = totalrecords;
      }

      iterationsGrid.DataSource = Job.Iterations;
    }


    protected void Page_Load(object sender, EventArgs e)
    {

    }

    public JobViewModel Job { get; private set; }

    protected void iterationsGrid_DetailTableDataBind(object sender, Telerik.Web.UI.GridDetailTableDataBindEventArgs e)
    {
      object item = e.DetailTableView.ParentItem.DataItem;
      if (item is IterationViewModel)
      {
        e.DetailTableView.DataSource = ((IterationViewModel)item).Faults;
      }
      else if (item is FaultViewModel)
      {
        FaultViewModel fault = item as FaultViewModel;
        if (e.DetailTableView.DataMember == "StateModel")
        {
          e.DetailTableView.DataSource = fault.StateModel;
        }
        else if (e.DetailTableView.DataMember == "CollectedData")
        {
          e.DetailTableView.DataSource = fault.CollectedData;
        }
      }
    }

    protected void iterationsGrid_ItemDataBound(object sender, Telerik.Web.UI.GridItemEventArgs e)
    {
      GridDataItem item = e.Item as GridDataItem;

      if (item != null)
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