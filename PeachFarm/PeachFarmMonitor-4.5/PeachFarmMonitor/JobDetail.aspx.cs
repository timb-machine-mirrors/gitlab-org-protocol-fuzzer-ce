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
    public JobDetail()
    {
      this.InitComplete += JobDetail_InitComplete;
    }

    void JobDetail_InitComplete(object sender, EventArgs e)
    {
      string jobid = Request.QueryString["jobid"];
      if (String.IsNullOrEmpty(jobid) == false)
      {
        monitorconfig = (PeachFarmMonitorSection)ConfigurationManager.GetSection("peachfarmmonitor");
        try
        {
          Job = new JobViewModel(Reports.Report.GetJobDetailReport(jobid, monitorconfig.MongoDb.ConnectionString).First());
        }
        catch { Job = null; }
      }
      else
      {
        lblJobID.Text = "No Job ID supplied";
        return;
      }

      if (Job == null)
      {
        lblJobID.Text = String.Format("Job {0} not found.", jobid);
        return;
      }
      else
      {
        string root = Server.MapPath(".");
        string archiveFolder = Path.Combine(root, "jobArchive");
        //Reports.FileWriter.CreateDirectory(archiveFolder);
        Reports.FileWriter.DumpFiles(monitorconfig.MongoDb.ConnectionString, archiveFolder, Job); 
        
        if ((iterationsGrid != null) && (iterationsGrid.DataSource == null))
        {
          lblJobID.Text = jobid;
          downloadOutputLink.NavigateUrl = "GetJobOutput.aspx?jobid=" + jobid;
          viewReportLink.NavigateUrl = "ReportViewer.aspx?jobid=" + jobid;
          Page.Title = "Job Detail: " + jobid;

          iterationsGrid.DataSource = Job.Iterations;
          iterationsGrid.DataBind();
        }
      }
    }
    private static PeachFarmMonitorSection monitorconfig = null;

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