﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using PeachFarm.Common.Mongo;

namespace PeachFarmMonitor.ViewModels
{
  public class FaultViewModel : Fault
  {
    public FaultViewModel(Fault fault)
    {
      this._id = fault._id;
      this.ControlIteration = fault.ControlIteration;
      this.ControlRecordingIteration = fault.ControlRecordingIteration;
      this.Description = fault.Description;
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

			this.GeneratedFiles = new List<GeneratedFileViewModel>();
			foreach (var gf in fault.GeneratedFiles)
			{
				this.GeneratedFiles.Add(new GeneratedFileViewModel(gf));
			}
    }

    public new List<GeneratedFileViewModel> GeneratedFiles { get; set; }
  }

}