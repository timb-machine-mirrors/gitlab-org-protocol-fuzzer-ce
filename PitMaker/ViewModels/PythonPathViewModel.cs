using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PitMaker.Models;
using System.ComponentModel;

namespace PitMaker.ViewModels
{
  class PythonPathViewModel : ViewModelBase
  {
    public PythonPathViewModel(PythonPath model, ViewModelBase parent)
      :base(model, parent)
    {

    }

    [Browsable(false)]
    public override string Image
    {
      get
      {
        return "Images/node-pythonpath.png";
      }
    }
  }
}
