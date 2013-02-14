using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PitMaker.Models;
using System.Dynamic;
using System.Windows;

namespace PitMaker.ViewModels
{
  class PublisherViewModel : ViewModelBase
  {
    public PublisherViewModel(Publisher model, ViewModelBase parent)
      :base(model, parent)
    {
      if (model.PeachType == null)
      {
        try
        {
          model.PeachType = PublisherAltNames[model.PublisherClass];
        }
        catch
        {
          throw new Peach.Core.PeachException("Publisher class '{0}' can not be found in loaded assemblies.", model.PublisherClass);
        }
      }
    }
  }
}
