using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PitMaker.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace PitMaker.ViewModels
{
  class StateModelViewModel : ViewModelBase
  {
    public StateModelViewModel(StateModel model, ViewModelBase parent)
      :base(model, parent)
    {
      this.Children = WrapItems();
    }

    protected ObservableCollection<ViewModelBase> WrapItems()
    {
      ObservableCollection<ViewModelBase> vms = new ObservableCollection<ViewModelBase>();
      if (((StateModel)this.Model).Items.Count > 0)
      {
        foreach (Models.Node item in ((StateModel)this.Model).Items)
        {
          ViewModelBase vm;
          if (item is Models.State)
          {
            vm = new StateViewModel((Models.State)item, this);
          }
          else
          {
            throw new Peach.Core.PeachException("Only States are valid children of State Models");
          }

          vms.Add(vm);
        }
      }
      return vms;
    }

    [Browsable(false)]
    public override string Image
    {
      get
      {
        return "Images/node-statemachine.png";
      }
    }

    internal override void ExecuteCreateChild(string parameter)
    {
      switch (parameter)
      {
        case "State":
          Models.State m = new State();
          ((StateModel)this.Model).Items.Add(m);
          this.Children.Add(new StateViewModel(m, this));
          RaisePropertyChanged("States");
          break;
      }
    }

    protected override void DeleteChild(ViewModelBase viewModel)
    {
      ((Models.StateModel)Model).Items.Remove((Models.State)viewModel.Model);
      this.Children.Remove(viewModel);
      RaisePropertyChanged("States");
    }

    [Browsable(false)]
    public ObservableCollection<State> States
    {
      get
      {
        return new ObservableCollection<State>(from StateViewModel svm in this.Children select svm.Model as State);
      }
    }
  }
}
