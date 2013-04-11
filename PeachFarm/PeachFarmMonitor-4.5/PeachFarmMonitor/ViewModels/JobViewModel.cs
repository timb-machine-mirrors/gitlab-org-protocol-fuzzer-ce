using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using PeachFarm.Common.Mongo;

namespace PeachFarmMonitor.ViewModels
{
  public class JobViewModel : Job
  {
    public JobViewModel(Job job, JobStatus status = JobStatus.Inactive)
    {
      this.JobID = job.JobID;
      this.Pit = job.Pit;
      this.StartDate = job.StartDate;
      this.UserName = job.UserName;
      this.PeachVersion = job.PeachVersion;
      this.Tags = job.Tags;

      this.Status = status;

      FaultBuckets = new List<FaultBucketViewModel>();
    }


    public new List<NodeViewModel> Nodes { get; set; }

    public List<FaultBucketViewModel> FaultBuckets { get; set; }

    public JobStatus Status { get; set; }

    public int FaultCount { get; set; }
  }

  public enum JobStatus
  {
    Running,
    Inactive
  }
}