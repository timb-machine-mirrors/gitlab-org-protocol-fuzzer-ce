using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using PeachFarm.Common.Mongo;

namespace PeachFarmMonitor.ViewModels
{
  public class NodeViewModel : Node
  {
    public NodeViewModel(Node node)
    {
      this._id = node._id;
      this.FaultCount = node.FaultCount;
      this.JobID = node.JobID;
      this.Name = node.Name;
      this.SeedNumber = node.SeedNumber;
      this.IterationCount = node.IterationCount;
      this.Tags = node.Tags;

      //TODO add fault code
    }
  }
}