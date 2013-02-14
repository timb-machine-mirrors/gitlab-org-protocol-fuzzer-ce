using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PitMaker.Models;
using System.ComponentModel;

namespace PitMaker.ViewModels
{
  class ReferenceViewModel : ViewModelBase
  {
    public ReferenceViewModel(ReferenceNode reference, ViewModelBase parent)
      :base(reference, parent)
    {
      this.DisplayName = reference.GetType().Name.Replace("ReferenceNode", String.Empty);
    }

    #region DisplayName Property

    private string displayNameField = String.Empty;

    [Browsable(false)]
    public string DisplayName
    {
      get
      {
        return this.displayNameField;
      }
      set
      {
        if (this.displayNameField != value)
        {
          this.displayNameField = value;
          RaisePropertyChanged("DisplayName");
        }
      }
    }

    #endregion


  }
}
