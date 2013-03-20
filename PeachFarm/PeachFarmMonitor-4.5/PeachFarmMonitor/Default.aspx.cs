using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using PeachFarm.Admin.Configuration;
using PeachFarm.Common;
using PeachFarm.Common.Mongo;
using Messages = PeachFarm.Common.Messages;
using PeachFarmMonitor.Configuration;
using PeachFarmMonitor.ViewModels;
using Telerik.Web.UI;

namespace PeachFarmMonitor
{
  public partial class Home : System.Web.UI.Page
  {
    private static PeachFarm.Admin.Admin admin = null;
    private static AdminSection adminconfig = null;
    private static PeachFarmMonitorSection monitorconfig = null;

    private string guid;
    public Home()
    {
      adminconfig = (AdminSection)ConfigurationManager.GetSection("peachfarm.admin");
      monitorconfig = (PeachFarmMonitorSection)ConfigurationManager.GetSection("peachfarmmonitor");
      guid = Guid.NewGuid().ToString();
    }

    protected void Page_Load(object sender, EventArgs e)
    {
      lblHost.Text = adminconfig.Controller.IpAddress;
    }

    #region Monitor
    private Task<Messages.MonitorResponse> MonitorAsync()
    {
      TaskCompletionSource<Messages.MonitorResponse> tcs = new TaskCompletionSource<Messages.MonitorResponse>();
      admin = new PeachFarm.Admin.Admin(String.Format(QueueNames.QUEUE_MONITOR, guid));
      admin.MonitorCompleted += (s, e) =>
        {
          if (e.Error != null)
            tcs.TrySetException(e.Error);
          else if (e.Cancelled)
            tcs.TrySetCanceled();
          else
            tcs.TrySetResult(e.Result);

        };
      admin.MonitorAsync();
      return tcs.Task;
    }

    private async void Monitor()
    {
      try
      {
        Messages.MonitorResponse response = await MonitorAsync();

        List<JobViewModel> alljobs = new List<JobViewModel>();
        alljobs.AddRange(from Messages.Job j in response.ActiveJobs select new JobViewModel(j, JobStatus.Running));
        alljobs.AddRange(from Messages.Job j in response.InactiveJobs select new JobViewModel(j));

        foreach (var j in alljobs)
        {
          j.FaultCount = ((Job)j).GetFaultCount(monitorconfig.MongoDb.ConnectionString);
        }

        jobsGrid.DataSource = alljobs;
        jobsGrid.DataBind();

        //lstInactiveJobs.DataSource = response.InactiveJobs;
        //lstInactiveJobs.DataBind();

        //lstNodes.DataSource = response.Nodes;
        //lstNodes.DataBind();

        nodesGrid.DataSource = response.Nodes;
        nodesGrid.DataBind();

        /*
        RadToolBarButton iconviewbutton = (RadToolBarButton)nodesToolbar.Items.FindItemByText("Icon");
        if (iconviewbutton.Checked)
        {
          gridNodes.CssClass = "hidden";
          lstNodes.CssClass = "";
        }
        else
        {
          lstNodes.CssClass = "hidden";
          gridNodes.CssClass = "";
        }
        //*/

        errorsGrid.DataSource = response.Errors;
        errorsGrid.DataBind();

        loadingLabel.Text = "";
      }
      catch(Exception ex)
      {
        loadingLabel.Text = ex.Message;
        loadingLabel.BackColor = System.Drawing.Color.Red;
      }
     

    }
    #endregion

    protected void RadAjaxManager1_AjaxRequest(object sender, Telerik.Web.UI.AjaxRequestEventArgs e)
    {
    }

    protected void Tick(object sender, EventArgs e)
    {
      //RadAjaxManager1.RaisePostBackEvent("Waiting for response...");

      Monitor();

      
    }

    protected void errorsGrid_ItemDataBound(object sender, GridItemEventArgs e)
    {
      GridDataItem item = e.Item as GridDataItem;
      
      if (item != null)
      {
        PeachFarm.Common.Messages.Heartbeat heartbeat = item.DataItem as PeachFarm.Common.Messages.Heartbeat;
        Label label = item.DetailTemplateItemDataCell.FindControl("ErrorMessage") as Label;
        if (label != null)
        {
          label.Text = heartbeat.ErrorMessage;
        }
      }
    }

    protected void nodesGrid_ItemDataBound(object sender, GridItemEventArgs e)
    {
      GridDataItem item = e.Item as GridDataItem;

      if (item != null)
      {
        Messages.Heartbeat heartbeat = item.DataItem as Messages.Heartbeat;
        switch (heartbeat.Status)
        {
          case Messages.Status.Alive:
            item.Style.Add("background-color", "lightblue");
            break;
          case Messages.Status.Running:
            item.Style.Add("background-color", "lightgreen");
            break;
          case Messages.Status.Late:
            item.Style.Add("background-color", "lightyellow");
            break;
        }
      }
    }

    protected void jobsGrid_ItemDataBound(object sender, GridItemEventArgs e)
    {
      GridDataItem item = e.Item as GridDataItem;

      if (item != null)
      {
        JobViewModel job = item.DataItem as JobViewModel;
        switch (job.Status)
        {
          case JobStatus.Running:
            item.Style.Add("background-color", "lightgreen");
            break;
        }
      }
    }

    //protected void jobsGrid_ItemCommand(object sender, GridCommandEventArgs e)
    //{
    //  GridDataItem item = e.Item as GridDataItem;
    //  switch (e.CommandName)
    //  {
    //    case "GetJobDetail":
    //      GetJobDetail(((LinkButton)e.CommandSource).Text);
    //      break;
    //  }
    //}

    //private void GetJobDetail(string jobID)
    //{
    //  PeachFarm.Common.Mongo.Job job = PeachFarm.Common.Mongo.DatabaseHelper.GetJob(jobID, monitorconfig.MongoDb.ConnectionString);
    //  string tabname = String.Format("{0}-{1}", job.JobID, job.PitFileName);
    //  RadTab newtab = new RadTab(tabname);
    //  tabstrip.Tabs.Add(newtab);

    //  RadPageView newpage = new RadPageView();
    //  newpage.ID = tabname;
      

    //  toplevel.PageViews.Add(newpage);
    //  newpage.Selected = true;
    //}

    //protected void tabstrip_TabClick(object sender, RadTabStripEventArgs e)
    //{
    //  switch (e.Tab.Text)
    //  {
    //    case "Nodes":
    //      nodesPage.Selected = true;
    //      break;
    //    case "Jobs":
    //      jobsPage.Selected = true;
    //      break;
    //    case "Errors":
    //      errorsPage.Selected = true;
    //      break;
    //    default:
    //      if (e.Tab.Text.IndexOf('-') == 12)
    //      {
    //        //string jobID = e.Tab.Text.Substring(0, 12);
    //        //RadPageView pageView = (RadPageView)Page.FindControl(jobID + "_detail");

    //        RadPageView pageView = (RadPageView)Page.FindControl(e.Tab.Text);
    //        pageView.Selected = true;
    //      }
    //      break;
    //  }
    //}

    //protected void toplevel_PageViewCreated(object sender, RadMultiPageEventArgs e)
    //{
    //  if (e.PageView.ID.IndexOf('-') == 12)
    //  {
    //    string jobID = e.PageView.ID.Substring(0, 12);

    //    Job job = Reports.Report.GetJobDetailReport(jobID, monitorconfig.MongoDb.ConnectionString).First();
    //    JobDetail jobDetail = (JobDetail)Page.LoadControl(typeof(JobDetail), new object[] { job });
    //    jobDetail.ID = jobID + "_detail";

    //    e.PageView.Controls.Add(jobDetail);
    //  }
    //}
  }
}