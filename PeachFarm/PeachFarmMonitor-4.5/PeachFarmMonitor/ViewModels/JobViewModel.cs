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

      Nodes = new List<NodeViewModel>();
      foreach (Node node in job.Nodes)
      {
        Nodes.Add(new NodeViewModel(node));
      }

      FaultBuckets = new List<FaultBucketViewModel>();
      if (job.Faults != null)
      {
        FaultCount = job.Faults.Count;
        FaultComparer comparer = new FaultComparer();
        var buckets = job.Faults.Distinct(comparer);
        foreach (var bucket in buckets)
        {
          var childfaults = (from f in job.Faults where comparer.Equals(f, bucket) select f).ToList();
          FaultBucketViewModel fbvm = new FaultBucketViewModel(bucket, childfaults);
          FaultBuckets.Add(fbvm);
        }
      }
    }

    public void FillFaults(List<Fault> faults)
    {
      FaultBuckets = new List<FaultBucketViewModel>();
      if (faults != null)
      {
        FaultCount = faults.Count;
        FaultComparer comparer = new FaultComparer();
        var buckets = faults.Distinct(comparer);
        foreach (var bucket in buckets)
        {
          var childfaults = (from f in faults where comparer.Equals(f, bucket) select f).ToList();
          FaultBucketViewModel fbvm = new FaultBucketViewModel(bucket, childfaults);
          FaultBuckets.Add(fbvm);
        }
      }
    }

    public List<NodeViewModel> Nodes { get; set; }

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