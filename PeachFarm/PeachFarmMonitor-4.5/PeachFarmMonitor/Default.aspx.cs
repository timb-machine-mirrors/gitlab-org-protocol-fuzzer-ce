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
using PeachFarm.Common.Messages;
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
    private Task<MonitorResponse> MonitorAsync()
    {
      TaskCompletionSource<MonitorResponse> tcs = new TaskCompletionSource<MonitorResponse>();
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
        MonitorResponse response = await MonitorAsync();

        List<JobViewModel> alljobs = new List<JobViewModel>();
        alljobs.AddRange(from Job j in response.ActiveJobs select new JobViewModel(j));
        alljobs.AddRange(from Job j in response.InactiveJobs select new JobViewModel(j));
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
        Heartbeat heartbeat = item.DataItem as Heartbeat;
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
        Heartbeat heartbeat = item.DataItem as Heartbeat;
        switch (heartbeat.Status)
        {
          case Status.Alive:
            item.Style.Add("background-color", "lightblue");
            break;
          case Status.Running:
            item.Style.Add("background-color", "lightgreen");
            break;
          case Status.Late:
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
  }
}