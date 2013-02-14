using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PitMaker.Models;
using System.Dynamic;
using System.ComponentModel;

namespace PitMaker.ViewModels
{
  class MonitorViewModel : ViewModelBase
  {
    public MonitorViewModel(Models.Monitor model, ViewModelBase parent)
      :base(model, parent)
    {
      if (model.PeachType == null)
      {
        try
        {
          model.PeachType = MonitorAltNames[model.MonitorClass];
        }
        catch
        {
          throw new Peach.Core.PeachException("Montor class '{0}' can not be found in loaded assemblies.", model.MonitorClass);
        }
      }
    }

    [Browsable(false)]
    public override string Image
    {
      get
      {
        return "Images/node-monitor.png";
      }
    }
  }
}
