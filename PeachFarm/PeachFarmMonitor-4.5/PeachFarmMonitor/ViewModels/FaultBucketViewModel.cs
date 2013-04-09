using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using PeachFarm.Common.Mongo;

namespace PeachFarmMonitor.ViewModels
{
  public class FaultBucketViewModel : Fault
  {
    public FaultBucketViewModel(Fault fault, List<Fault> childFaults)
    {
      if (String.IsNullOrEmpty(fault.FolderName) == false)
      {
        BucketName = fault.FolderName;
      }
      else if (String.IsNullOrEmpty(fault.MajorHash) && String.IsNullOrEmpty(fault.MinorHash) && String.IsNullOrEmpty(fault.Exploitability))
      {
        BucketName = "Unknown";
      }
      else
      {
        BucketName = string.Format("{0}_{1}_{2}", fault.Exploitability, fault.MajorHash, fault.MinorHash);
      }

      this.ControlIteration = fault.ControlIteration;
      this.ControlRecordingIteration = fault.ControlRecordingIteration;
      this.DetectionSource = fault.DetectionSource;
      this.Exploitability = fault.Exploitability;
      this.FaultType = fault.FaultType;
      this.FolderName = fault.FolderName;
      this.IsReproduction = fault.IsReproduction;
      this.Iteration = fault.Iteration;
      this.JobID = fault.JobID;
      this.MajorHash = fault.MajorHash;
      this.MinorHash = fault.MinorHash;
      this.NodeName = fault.NodeName;
      this.SeedNumber = fault.SeedNumber;
      this.Stamp = fault.Stamp;
      this.TestName = fault.TestName;
      this.Title = fault.Title;

      Faults = new List<FaultViewModel>();
      foreach (var child in childFaults)
      {
        Faults.Add(new FaultViewModel(child));
      }
    }

    public string BucketName { get; set; }

    public List<FaultViewModel> Faults { get; set; }
  }

  public class FaultComparer : EqualityComparer<Fault>
  {
    public override bool Equals(Fault x, Fault y)
    {
      string first = BuildString(x);
      string second = BuildString(y);

      return first.Equals(second);

    }

    private string BuildString(Fault x)
    {
      if (String.IsNullOrEmpty(x.FolderName) == false)
      {
        return x.FolderName;
      }
      else if (String.IsNullOrEmpty(x.MajorHash) && String.IsNullOrEmpty(x.MinorHash) && String.IsNullOrEmpty(x.Exploitability))
      {
        return "Unknown";
      }
      else
      {
        return string.Format("{0}_{1}_{2}", x.Exploitability, x.MajorHash, x.MinorHash);
      }

    }

    public override int GetHashCode(Fault obj)
    {
      return BuildString(obj).GetHashCode();
    }
  }

}