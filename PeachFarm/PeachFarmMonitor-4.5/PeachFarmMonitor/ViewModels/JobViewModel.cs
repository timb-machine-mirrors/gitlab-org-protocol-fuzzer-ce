using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using PeachFarm.Common.Mongo;

namespace PeachFarmMonitor.ViewModels
{
  public class JobViewModel : Job
  {
    public JobViewModel(PeachFarm.Common.Messages.Job job, JobStatus status = JobStatus.Inactive)
    {
      this.JobID = job.JobID;
      this.PitFileName = job.PitFileName;
      this.StartDate = job.StartDate;
      this.UserName = job.UserName;
      this.Status = status;
    }

    public JobViewModel(Job job, JobStatus status = JobStatus.Inactive)
    {
      this.JobID = job.JobID;
      this.PitFileName = job.PitFileName;
      this.StartDate = job.StartDate;
      this.UserName = job.UserName;
      this.Status = status;

      this.Iterations = new List<IterationViewModel>();
      foreach (var i in job.Iterations)
      {
        this.Iterations.Add(new IterationViewModel(i));
      }
    }

    public JobStatus Status { get; set; }

    public new List<IterationViewModel> Iterations { get; set; }

    public long FaultCount { get; set; }
  }

  public enum JobStatus
  {
    Running,
    Inactive
  }
}