using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PitMaker.Models;

namespace PitMaker.ViewModels
{
  class HintViewModel : ViewModelBase
  {
    public HintViewModel(Hint model, ViewModelBase parent)
      :base(model, parent)
    {

    }
  }
}
