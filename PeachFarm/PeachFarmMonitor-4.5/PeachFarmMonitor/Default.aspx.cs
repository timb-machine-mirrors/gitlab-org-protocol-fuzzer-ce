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
				//refreshTimer = new System.Timers.Timer(10000);
				//refreshTimer.Elapsed += (o, a) => { RadAjaxManager1.RaisePostBackEvent(String.Empty); };
				//refreshTimer.Start();

				//upPitFile.AllowedFileExtensions = new string[1] { ".xml" };

				chkSelectByCount.Attributes.Add("onClick", "SelectBy(\"count\");");
				chkSelectByTags.Attributes.Add("onClick", "SelectBy(\"tags\");");

				lstCount.Attributes.Add("style", "display: block");
				lstTags.Attributes.Add("style", "display: none");
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

				var aliveNodes = (from Messages.Heartbeat h in onlinenodes where h.Status == Messages.Status.Alive select h);
				var aliveNodesCount = aliveNodes.Count();
        aliveNodesLabel.Text = aliveNodesCount.ToString();
        runningNodesLabel.Text = (from Messages.Heartbeat h in onlinenodes where h.Status == Messages.Status.Running select h).Count().ToString();
        lateNodesLabel.Text = (from Messages.Heartbeat h in onlinenodes where h.Status == Messages.Status.Late select h).Count().ToString();

				lstCount.Items.Clear();
				for (int i = 1; i <= aliveNodesCount; i++)
				{
					lstCount.Items.Add(new DropDownListItem(i.ToString()));
				}

				var aliveNodeTags = aliveNodes.Select(n => n.Tags.Split(',')).SelectMany(s => s).Distinct().OrderBy(t => t);
				lstTags.Items.AddRange(aliveNodeTags.Select(t => new ListItem(t)).ToArray());
        #endregion

				#region errors
				errors = DatabaseHelper.GetAllErrors(monitorconfig.MongoDb.ConnectionString);
	      foreach (var error in errors)
	      {
		      error.ErrorMessage = HttpUtility.HtmlEncode(error.ErrorMessage);
	      }
				errorsGrid.DataSource = errors;
				errorsGrid.DataBind();
				#endregion

        #region jobs
				//*
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

					if ((from e in errors where e.JobID == job.JobID select e).Count() > 0)
					{
						jvm.ErrorsOccurred = true;
						jvm.Status = JobStatus.Error;
					}

          jvms.Add(jvm);
        }
        jobsGrid.DataSource = jvms;
        jobsGrid.DataBind();
				//*/
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
				if (item.DataItem is JobViewModel)
				{
					JobViewModel job = item.DataItem as JobViewModel;
					switch (job.Status)
					{
						case JobStatus.Running:
							item.Style.Add("background-color", "lightgreen");
							var button = item["StopJobButton"].Controls[0];
							break;
						case JobStatus.Error:
							item.Style.Add("background-color", "#FF8080");
							break;
					}

					if (job.Status != JobStatus.Running)
					{
						//item["StopJobButton"].Controls[0].Visible = false;
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
						else if (job.Status == JobStatus.Error)
						{
							dr.Text = "Unavailable";
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
				else if (item.DataItem is NodeViewModel)
				{

				}
      }
    }

    protected void nodesGrid_SortCommand(object sender, GridSortCommandEventArgs e)
    {
			if (e.NewSortOrder == GridSortOrder.Descending)
			{
				switch (e.SortExpression)
				{
					case "Status":
						nodesGrid.DataSource = (from n in onlinenodes orderby n.Status descending select n);
						break;
					case "NodeName":
						nodesGrid.DataSource = (from n in onlinenodes orderby n.NodeName descending select n);
						break;
					case "Stamp":
						nodesGrid.DataSource = (from n in onlinenodes orderby n.Stamp descending select n);
						break;
					case "Tags":
						nodesGrid.DataSource = (from n in onlinenodes orderby n.Tags descending select n);
						break;
					case "JobID":
						nodesGrid.DataSource = (from n in onlinenodes orderby n.JobID descending select n);
						break;
					case "PitFileName":
						nodesGrid.DataSource = (from n in onlinenodes orderby n.PitFileName descending select n);
						break;
					case "Seed":
						nodesGrid.DataSource = (from n in onlinenodes orderby n.Seed descending select n);
						break;
					case "Iteration":
						nodesGrid.DataSource = (from n in onlinenodes orderby n.Iteration descending select n);
						break;
				}
			}
			else
			{
				switch (e.SortExpression)
				{
					case "Status":
						nodesGrid.DataSource = (from n in onlinenodes orderby n.Status select n);
						break;
					case "NodeName":
						nodesGrid.DataSource = (from n in onlinenodes orderby n.NodeName select n);
						break;
					case "Stamp":
						nodesGrid.DataSource = (from n in onlinenodes orderby n.Stamp select n);
						break;
					case "Tags":
						nodesGrid.DataSource = (from n in onlinenodes orderby n.Tags select n);
						break;
					case "JobID":
						nodesGrid.DataSource = (from n in onlinenodes orderby n.JobID select n);
						break;
					case "PitFileName":
						nodesGrid.DataSource = (from n in onlinenodes orderby n.PitFileName select n);
						break;
					case "Seed":
						nodesGrid.DataSource = (from n in onlinenodes orderby n.Seed select n);
						break;
					case "Iteration":
						nodesGrid.DataSource = (from n in onlinenodes orderby n.Iteration select n);
						break;
				}
			}
		}

    protected void jobsGrid_SortCommand(object sender, GridSortCommandEventArgs e)
    {
			if (e.NewSortOrder == GridSortOrder.Descending)
			{
				switch (e.SortExpression)
				{
					case "FaultCount":
						jobsGrid.DataSource = (from jvm in jvms orderby jvm.FaultCount descending select jvm);
						break;
					case "IterationCount":
						jobsGrid.DataSource = (from jvm in jvms orderby jvm.IterationCount descending select jvm);
						break;
					case "JobID":
						jobsGrid.DataSource = (from jvm in jvms orderby jvm.JobID descending select jvm);
						break;
					case "StartDate":
						jobsGrid.DataSource = (from jvm in jvms orderby jvm.StartDate descending select jvm);
						break;
					case "Status":
						jobsGrid.DataSource = (from jvm in jvms orderby jvm.Status descending select jvm);
						break;
					case "Pit.FileName":
						jobsGrid.DataSource = (from jvm in jvms orderby jvm.Pit.FileName descending select jvm);
						break;
					case "UserName":
						jobsGrid.DataSource = (from jvm in jvms orderby jvm.UserName descending select jvm);
						break;
				}
			}
			else
			{
				switch (e.SortExpression)
				{
					case "FaultCount":
						jobsGrid.DataSource = (from jvm in jvms orderby jvm.FaultCount select jvm);
						break;
					case "IterationCount":
						jobsGrid.DataSource = (from jvm in jvms orderby jvm.IterationCount select jvm);
						break;
					case "JobID":
						jobsGrid.DataSource = (from jvm in jvms orderby jvm.JobID select jvm);
						break;
					case "StartDate":
						jobsGrid.DataSource = (from jvm in jvms orderby jvm.StartDate select jvm);
						break;
					case "Status":
						jobsGrid.DataSource = (from jvm in jvms orderby jvm.Status select jvm);
						break;
					case "Pit.FileName":
						jobsGrid.DataSource = (from jvm in jvms orderby jvm.Pit.FileName select jvm);
						break;
					case "UserName":
						jobsGrid.DataSource = (from jvm in jvms orderby jvm.UserName select jvm);
						break;
				}
			}
    }

    protected void errorsGrid_SortCommand(object sender, GridSortCommandEventArgs e)
    {

							//			<telerik:GridBoundColumn DataField="NodeName" HeaderText="Name" />
							//<telerik:GridBoundColumn DataField="Stamp" HeaderText="Last Update" />
							//<telerik:GridBoundColumn DataField="JobID" HeaderText="Job ID" />
							//<telerik:GridBoundColumn DataField="PitFileName" HeaderText="Pit File" />

			if (e.NewSortOrder == GridSortOrder.Descending)
			{
				switch (e.SortExpression)
				{
					case "NodeName":
						errorsGrid.DataSource = from err in errors orderby err.NodeName descending select err;
						break;
					case "Stamp":
						errorsGrid.DataSource = from err in errors orderby err.NodeName descending select err;
						break;
					case "JobID":
						errorsGrid.DataSource = from err in errors orderby err.NodeName descending select err;
						break;
					case "PitFileName":
						errorsGrid.DataSource = from err in errors orderby err.NodeName descending select err;
						break;
				}
			}
			else
			{
				switch (e.SortExpression)
				{
					case "NodeName":
						errorsGrid.DataSource = from err in errors orderby err.NodeName select err;
						break;
					case "Stamp":
						errorsGrid.DataSource = from err in errors orderby err.NodeName select err;
						break;
					case "JobID":
						errorsGrid.DataSource = from err in errors orderby err.NodeName select err;
						break;
					case "PitFileName":
						errorsGrid.DataSource = from err in errors orderby err.NodeName select err;
						break;
				}
			}
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

		protected void chkSelectByCount_CheckedChanged(object sender, EventArgs e)
		{
			lstCount.Visible = chkSelectByCount.Checked;
			lstTags.Visible = chkSelectByTags.Checked;
		}

		protected void jobsGrid_ItemCommand(object sender, GridCommandEventArgs e)
		{

		}

		protected void jobsGrid_DetailTableDataBind(object sender, GridDetailTableDataBindEventArgs e)
		{
			GridDataItem parent = e.DetailTableView.ParentItem;
			if ((parent != null) && (parent.DataItem != null))
			{
				switch (e.DetailTableView.DataMember)
				{
					case "Nodes":
						var job = ((JobViewModel)parent.DataItem);
						//if((job.Nodes == null) || (job.Nodes.Count == 0))
						//{
						//	job.FillNodes(monitorconfig.MongoDb.ConnectionString);
						//}
						e.DetailTableView.DataSource = job.Nodes;
						break;
				}
			}
		}

		protected void jobsGrid_NeedDataSource(object sender, GridNeedDataSourceEventArgs e)
		{
			/*
			var jobs = DatabaseHelper.GetAllJobs(monitorconfig.MongoDb.ConnectionString);
			var activejobids = (from h in onlinenodes where h.Status == Messages.Status.Running select h.JobID).ToList();
			jvms = new List<JobViewModel>();
			foreach (var job in jobs)
			{
				job.FillNodes(monitorconfig.MongoDb.ConnectionString);

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

				if ((from err in errors where err.JobID == job.JobID select err).Count() > 0)
				{
					jvm.ErrorsOccurred = true;
					jvm.Status = JobStatus.Error;
				}

				jvms.Add(jvm);
			}
			jobsGrid.DataSource = jvms;
			//*/
		}

  }
}