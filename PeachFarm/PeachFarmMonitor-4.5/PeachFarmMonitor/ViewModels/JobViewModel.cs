using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using PeachFarm.Common.Messages;

namespace PeachFarmMonitor.ViewModels
{
  public class JobViewModel : Job
  {
    public JobViewModel(Job job, JobStatus status = JobStatus.Inactive)
    {
      this.JobID = job.JobID;
      this.PitFileName = job.PitFileName;
      this.StartDate = job.StartDate;
      this.UserName = job.UserName;
      this.Status = status;
    }

    public JobStatus Status { get; set; }

  }

  public enum JobStatus
  {
    Running,
    Inactive
  }
}