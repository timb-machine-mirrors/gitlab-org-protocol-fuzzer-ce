using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PitMaker.Models;
using System.Dynamic;
using System.ComponentModel;

namespace PitMaker.ViewModels
{
  class LoggerViewModel : ViewModelBase
  {
    public LoggerViewModel(Logger model, ViewModelBase parent)
      :base(model, parent)
    {
      if (model.PeachType == null)
      {
        try
        {
          model.PeachType = LoggerAltNames[model.LoggerClass];
        }
        catch
        {
          throw new Peach.Core.PeachException("Logger class '{0}' can not be found in loaded assemblies.", model.LoggerClass);
        }
      }
    }

    [Browsable(false)]
    public override string Image
    {
      get
      {
        return "Images/node-logger.png";
      }
    }
  }
}
