using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PitMaker.Models;
using System.ComponentModel;

namespace PitMaker.ViewModels
{
  class ImportViewModel : ViewModelBase
  {
    public ImportViewModel(Import model, ViewModelBase parent)
      :base(model, parent)
    {

    }

    [Browsable(false)]
    public override string Image
    {
      get
      {
        return "Images/node-import.png";
      }
    }
  }
}
