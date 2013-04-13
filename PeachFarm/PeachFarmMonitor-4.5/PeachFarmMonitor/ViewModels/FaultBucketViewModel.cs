using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using PeachFarm.Common.Mongo;

namespace PeachFarmMonitor.ViewModels
{
  public class FaultBucketViewModel : Fault
  {
    public FaultBucketViewModel(Fault fault)
    {
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
      this.Group = fault.Group;

    }

    public int FaultCount { get; set; }
  }

  public class FaultComparer : EqualityComparer<Fault>
  {
    public override bool Equals(Fault x, Fault y)
    {
      return x.Group.Equals(y.Group);
    }

    public override int GetHashCode(Fault obj)
    {
      return obj.Group.GetHashCode();
    }
  }

}