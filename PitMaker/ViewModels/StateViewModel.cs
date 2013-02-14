using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace PitMaker.ViewModels
{
  class StateViewModel : ViewModelBase
  {
    public StateViewModel(Models.State model, ViewModelBase parent)
      :base(model, parent)
    {
      this.Children = WrapItems();
    }

    protected ObservableCollection<ViewModelBase> WrapItems()
    {
      ObservableCollection<ViewModelBase> vms = new ObservableCollection<ViewModelBase>();
      if (((Models.State)this.Model).Items.Count > 0)
      {
        foreach (Models.Node item in ((Models.State)this.Model).Items)
        {
          ViewModelBase vm;
          if (item is Models.Action)
          {
            vm = ActionViewModel.Create((Models.Action)item, this);
          }
          else
          {
            throw new Peach.Core.PeachException("Only Actions are valid children of States");
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
        return "Images/node-state.png";
      }
    }

    internal override void ExecuteCreateChild(string parameter)
    {
      Models.Action m = new Models.Action((PitMaker.Models.ActionType)Enum.Parse(typeof(PitMaker.Models.ActionType), parameter, true));
      ((Models.State)this.Model).Items.Add(m);
      ActionViewModel vm = ActionViewModel.Create(m, this);
      this.Children.Add(vm);

      #region old create code
      /*
      switch (parameter)
      {
        case "Accept":
          {
            Models.AcceptAction m = new Models.AcceptAction();
            ((Models.State)this.Model).Items.Add(m);
            this.Children.Add(new ActionViewModel(m, this));
            break;
          }
        case "Call":
          {
            Models.CallAction m = new Models.CallAction();
            ((Models.State)this.Model).Items.Add(m);
            this.Children.Add(new CallActionViewModel(m, this));
            break;
          }
        case "ChangeState":
          {
            Models.ChangeStateAction m = new Models.ChangeStateAction();
            ((Models.State)this.Model).Items.Add(m);
            this.Children.Add(new ActionViewModel(m, this));
            break;
          }
        case "Open":
          {
            Models.OpenAction m = new Models.OpenAction();
            ((Models.State)this.Model).Items.Add(m);
            this.Children.Add(new ActionViewModel(m, this));
            break;
          }
        case "Close":
          {
            Models.CloseAction m = new Models.CloseAction();
            ((Models.State)this.Model).Items.Add(m);
            this.Children.Add(new ActionViewModel(m, this));
            break;
          }
        case "Connect":
          {
            Models.ConnectAction m = new Models.ConnectAction();
            ((Models.State)this.Model).Items.Add(m);
            this.Children.Add(new ActionViewModel(m, this));
            break;
          }
        case "Start":
          {
            Models.StartAction m = new Models.StartAction();
            ((Models.State)this.Model).Items.Add(m);
            this.Children.Add(new ActionViewModel(m, this));
            break;
          }
        case "Stop":
          {
            Models.StopAction m = new Models.StopAction();
            ((Models.State)this.Model).Items.Add(m);
            this.Children.Add(new ActionViewModel(m, this));
            break;
          }
        case "Slurp":
          {
            Models.SlurpAction m = new Models.SlurpAction();
            ((Models.State)this.Model).Items.Add(m);
            this.Children.Add(new ActionViewModel(m, this));
            break;
          }
        case "GetProperty":
          {
            Models.GetPropAction m = new Models.GetPropAction();
            ((Models.State)this.Model).Items.Add(m);
            this.Children.Add(new GetPropActionViewModel(m, this));
            break;
          }
        case "SetProperty":
          {
            Models.SetPropAction m = new Models.SetPropAction();
            ((Models.State)this.Model).Items.Add(m);
            this.Children.Add(new SetPropActionViewModel(m, this));
            break;
          }
        case "Input":
          {
            Models.InputAction m = new Models.InputAction();
            ((Models.State)this.Model).Items.Add(m);
            this.Children.Add(new InputActionViewModel(m, this));
            break;
          }
        case "Output":
          {
            Models.OutputAction m = new Models.OutputAction();
            ((Models.State)this.Model).Items.Add(m);
            this.Children.Add(new OutputActionViewModel(m, this));
            break;
          }
      }
      //*/
      #endregion
    }

    protected override void DeleteChild(ViewModelBase viewModel)
    {
      ((Models.State)Model).Items.Remove((Models.Action)viewModel.Model);
      this.Children.Remove(viewModel);
    }

    public override void ReorderItems()
    {
      ObservableCollection<object> newItems = new ObservableCollection<object>();
      foreach (ViewModelBase vm in this.Children)
      {
        newItems.Add(vm.Model);
      }
      ((Models.State)Model).Items = newItems;
    }
  }
}
