using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PitMaker.Models;
using System.Dynamic;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace PitMaker.ViewModels
{
  class FixupViewModel : ViewModelBase
  {
    public FixupViewModel(Fixup model, ViewModelBase parent)
      :base(model, parent)
    {
      if (model.PeachType == null)
      {
        model.PeachType = (from Type t in this.FixupTypes where t.Name == model.FixupClass select t).FirstOrDefault();
        if (model.PeachType == null)
        {
          throw new Peach.Core.PeachException("Fixup class '{0}' can not be found in loaded assemblies.", model.FixupClass);
        }
      }
    }

    [Browsable(false)]
    public DataModelViewModel DataModel
    {
      get { return FindDataModel(this); }
    }

    private DataModelViewModel FindDataModel(ViewModelBase viewModel)
    {
      if (viewModel is DataModelViewModel)
        return viewModel as DataModelViewModel;
      else if (viewModel.Parent != null)
        return FindDataModel(viewModel.Parent);
      else
      {
        throw new Peach.Core.PeachException("View Model parenting error. Fix me.");
      }
    }
  }
}
