using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PitMaker.Models;
using System.ComponentModel;

namespace PitMaker.ViewModels
{
  class TestIncludeExcludeViewModel : ViewModelBase
  {
    public TestIncludeExcludeViewModel(Node node, ViewModelBase parent)
      :base(node, parent)
    {
      if (node is TestInclude)
      {
        this.DisplayName = "Include";
      }
      else if (node is TestExclude)
      {
        this.DisplayName = "Exclude";
      }
      else
      {
        throw new Peach.Core.PeachException("Only Include and Exclude are valid children");
      }
    }

    #region DisplayName Property

    private string displayNameField;

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
