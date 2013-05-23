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
using PeachFarm.Common;
using PeachFarm.Common.Mongo;
using Messages = PeachFarm.Common.Messages;
using PeachFarmMonitor.Configuration;
using PeachFarmMonitor.ViewModels;
using Telerik.Web.UI;
using RabbitMQ.Client.Exceptions;
using MongoDB.Driver.Builders;


namespace PeachFarmMonitor
{
  public partial class Home : BasePage
  {
    private static PeachFarmMonitorSection monitorconfig = null;

    private static NLog.Logger nlog = NLog.LogManager.GetCurrentClassLogger();

    private static List<Messages.Heartbeat> onlinenodes;
    private static List<JobViewModel> jvms;
    private static List<Messages.Heartbeat> errors;
		private System.Timers.Timer refreshTimer;
    public Home()
    {
      monitorconfig = (PeachFarmMonitorSection)ConfigurationManager.GetSection("peachfarmmonitor");
    }

    protected void Page_Load(object sender, EventArgs e)
    {
      if (!Page.IsPostBack)
      {
        //lblHost.Text = String.Format("{0}", adminconfig.Controller.IpAddress);
        Monitor(true);
				refreshTimer = new System.Timers.Timer(10000);
				refreshTimer.Elapsed += (o, a) => { RadAjaxManager1.RaisePostBackEvent(String.Empty); };
				refreshTimer.Start();
      }
    }

    #region Monitor

    private void Monitor(bool displayError = false)
    {
      try
      {
        #region nodes
        onlinenodes = DatabaseHelper.GetAllNodes(monitorconfig.MongoDb.ConnectionString);
        nodesGrid.DataSource = onlinenodes;
        nodesGrid.DataBind();

        var activejobids = (from h in onlinenodes where h.Status == Messages.Status.Running select h.JobID).ToList();

        aliveNodesLabel.Text = (from Messages.Heartbeat h in onlinenodes where h.Status == Messages.Status.Alive select h).Count().ToString();
        runningNodesLabel.Text = (from Messages.Heartbeat h in onlinenodes where h.Status == Messages.Status.Running select h).Count().ToString();
        lateNodesLabel.Text = (from Messages.Heartbeat h in onlinenodes where h.Status == Messages.Status.Late select h).Count().ToString();
        #endregion

        #region jobs
        List<Job> jobs = DatabaseHelper.GetAllJobs(monitorconfig.MongoDb.ConnectionString);
        jvms = new List<JobViewModel>();
        foreach (Job job in jobs)
        {
          job.FillNodes(monitorconfig.MongoDb.ConnectionString);
          //job.FillFaults(monitorconfig.MongoDb.ConnectionString);

          job.StartDate = job.StartDate.ToLocalTime();

          JobViewModel jvm = null;
          if (activejobids.Contains(job.JobID))
          {
            jvm = new JobViewModel(job, JobStatus.Running);
          }
          else
          {
            jvm = new JobViewModel(job);
          }
          //var collection = DatabaseHelper.GetCollection<Fault>(MongoNames.Faults, monitorconfig.MongoDb.ConnectionString);
          //jvm.FaultCount = Convert.ToUInt32(collection.Distinct("_id", Query.EQ("JobID", job.JobID)).Count());
          //jvm.IterationCount = Convert.ToUInt32((from n in job.Nodes select Convert.ToDecimal(n.IterationCount)).Sum());
          //collection.Database.Server.Disconnect();
          jvms.Add(jvm);
        }
        jobsGrid.DataSource = jvms;
        jobsGrid.DataBind();
        #endregion

        #region errors
        errors = DatabaseHelper.GetAllErrors(monitorconfig.MongoDb.ConnectionString);
        errorsGrid.DataSource = errors;
        errorsGrid.DataBind();
        #endregion

        loadingLabel.Text = "";
        loadingLabel.Visible = false;

				
      }
      catch (Exception ex)
      {
        string message = ex.Message;
        if (ex.InnerException != null)
        {
          message += "\n" + ex.InnerException.Message;
        }
        nlog.Warn(message);

        if (displayError)
        {
          loadingLabel.Text = "ERROR";
          loadingLabel.BackColor = System.Drawing.Color.Red;
          loadingLabel.Visible = true;
        }
      }
    }

    #endregion

    protected void Tick(object sender, EventArgs e)
    {
      Monitor();
    }

    protected void errorsGrid_ItemDataBound(object sender, GridItemEventArgs e)
    {
      GridDataItem item = e.Item as GridDataItem;
      
      if (item != null)
      {
        PeachFarm.Common.Messages.Heartbeat heartbeat = item.DataItem as PeachFarm.Common.Messages.Heartbeat;
        var panelbar = item.DetailTemplateItemDataCell.FindControl("messagePanel") as RadPanelBar;
        var panelitem = panelbar.Items[0];
        var newline = heartbeat.ErrorMessage.IndexOf("\n");

        if (newline > 0)
        {
          panelitem.Text = String.Format("Message: {0} ... (click for more)", heartbeat.ErrorMessage.Substring(0,newline));
        }
        else
        {
          panelitem.Text = String.Format("Message: {0}", heartbeat.ErrorMessage);
        }
        var label = panelitem.FindControl("ErrorMessage") as TextBox;
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

				var dr = item.FindControl("linkDownloadReport") as HyperLink;
				if ((dr != null) && (String.IsNullOrEmpty(job.ReportLocation)))
				{
					dr.NavigateUrl = String.Empty;
					dr.Target = String.Empty;

					if (job.Status == JobStatus.Running)
					{
						dr.Text = "Waiting for Job completion.";
					}
					else
					{
						dr.Text = "Processing";
					}
				}
				else
				{
					dr.Text = "Download";
					dr.NavigateUrl = "GetJobOutput.aspx?file=" + job.ReportLocation;
					dr.Target = "_blank";
				}
      }
    }

    protected void nodesGrid_SortCommand(object sender, GridSortCommandEventArgs e)
    {
    }

    protected void jobsGrid_SortCommand(object sender, GridSortCommandEventArgs e)
    {
    }

    protected void errorsGrid_SortCommand(object sender, GridSortCommandEventArgs e)
    {
    }


    protected void tabstrip_TabClick(object sender, RadTabStripEventArgs e)
    {
      switch (e.Tab.Text)
      {
        case "Nodes":
          nodesPage.Selected = true;
          break;
        case "Jobs":
          jobsPage.Selected = true;
          break;
        case "Errors":
          errorsPage.Selected = true;
          break;
      }
    }

		protected void RadAjaxManager1_AjaxRequest(object sender, AjaxRequestEventArgs e)
		{
			//Monitor(true);
			RadAjaxManager1.Alert("yay");
		}
  }
}