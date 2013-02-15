using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PitMaker.Models;
using System.Dynamic;

namespace PitMaker.ViewModels
{
  class AnalyzerViewModel : ViewModelBase
  {
    public AnalyzerViewModel(Analyzer model, ViewModelBase parent)
      :base(model, parent)
    {
      if (model.PeachType == null)
      {
        model.PeachType = (from Type t in this.AnalyzerTypes where t.Name == model.AnalyzerClass select t).FirstOrDefault();
        if (model.PeachType == null)
        {
          throw new Peach.Core.PeachException("Analyzer class '" + model.AnalyzerClass + "' can not be found in loaded assemblies.");
        }
      }
    }
  }
}
