using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PitMaker.Models;
using System.Dynamic;
using Peach.Core;

namespace PitMaker.ViewModels
{
  class StrategyViewModel : ViewModelBase
  {
    public StrategyViewModel(Strategy model, ViewModelBase parent)
      :base(model, parent)
    {
      if (model.PeachType == null)
      {
        model.PeachType = (from Type t in this.StrategyTypes where t.Name == model.StrategyClass select t as Type).First();
        if (model.PeachType == null)
        {
          throw new Peach.Core.PeachException("Strategy class '{0}' can not be found in loaded assemblies.", model.StrategyClass);
        }
      }
    }
  }
}
