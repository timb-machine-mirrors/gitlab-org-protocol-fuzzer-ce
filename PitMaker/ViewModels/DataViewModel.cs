using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PitMaker.Models;
using System.Windows.Input;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Win32;
using System.ComponentModel;

namespace PitMaker.ViewModels
{
  class DataViewModel : ViewModelBase
  {
    public DataViewModel(Data model, ViewModelBase parent)
      :base(model, parent)
    {
    }

    [Browsable(false)]
    public override string Image
    {
      get
      {
        return "Images/node-data.png";
      }
    }
  }
}
