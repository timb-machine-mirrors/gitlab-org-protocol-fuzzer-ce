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
        
        lstJobs.DataSource = response.ActiveJobs;
        lstJobs.DataBind();

        lstInactiveJobs.DataSource = response.InactiveJobs;
        lstInactiveJobs.DataBind();

        lstNodes.DataSource = response.Nodes;
        lstNodes.DataBind();

        lstErrors.DataSource = response.Errors;
        lstErrors.DataBind();

        loadingLabel.Text = "";
      }
      catch(Exception ex)
      {
        loadingLabel.Text = ex.Message;
      }
     

    }
    #endregion

    protected void RadAjaxManager1_AjaxRequest(object sender, Telerik.Web.UI.AjaxRequestEventArgs e)
    {
      //lblError.Text = e.Argument;
    }

    protected void Tick(object sender, EventArgs e)
    {
      //RadAjaxManager1.RaisePostBackEvent("Waiting for response...");

      Monitor();

      
    }
  }
}