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
      this.ZipFile = job.ZipFile;

      this.Status = status;

      //if (String.IsNullOrEmpty(job.ZipFile))
      //{
      //  JobInput = String.Format("{0}\\{1}.xml", job.JobID, job.Pit.FileName);
      //}
      //else
      //{
      //  JobInput = job.ZipFile;
      //}

      Nodes = new List<NodeViewModel>();
      if (job.Nodes != null)
      {
        foreach (var node in job.Nodes)
        {
          Nodes.Add(new NodeViewModel(node));
          IterationCount += node.IterationCount;
          FaultCount += node.FaultCount;
        }

        FaultBuckets = new List<FaultBucketViewModel>();

      }
    }


    public new List<NodeViewModel> Nodes { get; set; }

    public List<FaultBucketViewModel> FaultBuckets { get; set; }

    public JobStatus Status { get; set; }

    public uint FaultCount { get; set; }

    public uint IterationCount { get; set; }

    //public string JobInput { get; set; }
  }

  public enum JobStatus
  {
    Running,
    Inactive
  }
}