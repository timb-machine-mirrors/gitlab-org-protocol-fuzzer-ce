using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PitMaker.Models;
using System.Collections.ObjectModel;

namespace PitMaker.ViewModels
{
  class DefaultsViewModel : ViewModelBase
  {
    public DefaultsViewModel(Defaults model, ViewModelBase parent)
      :base(model, parent)
    {
      this.Children = WrapItems();
    }
    protected ObservableCollection<ViewModelBase> WrapItems()
    {
      ObservableCollection<ViewModelBase> vms = new ObservableCollection<ViewModelBase>();

      if (((Defaults)this.Model).Items.Count > 0)
      {
        
        foreach (Default child in ((Defaults)this.Model).Items)
        {
          switch (child.GetType().Name)
          {
            case "DefaultNumber":
              vms.Add(new DefaultViewModel((DefaultNumber)child, this));
              break;
            case "DefaultString":
              vms.Add(new DefaultViewModel((DefaultString)child, this));
              break;
            case "DefaultFlags":
              vms.Add(new DefaultViewModel((DefaultFlags)child, this));
              break;
            case "DefaultBlob":
              vms.Add(new DefaultViewModel((DefaultBlob)child, this));
              break;
          }
        }
      }
      return vms;
    }
    internal override void ExecuteCreateChild(string parameter)
    {
      if (HasDefault(parameter))
      {
        throw new Peach.Core.PeachException("Only one default per type is allowed.");
      }
      else
      {
        switch (parameter)
        {
          case "DefaultNumber":
            DefaultNumber n = new DefaultNumber();
            ((Defaults)Model).Items.Add(n);
            this.Children.Add(new DefaultViewModel(n, this));
            break;
          case "DefaultString":
            DefaultString s = new DefaultString();
            ((Defaults)Model).Items.Add(s);
            this.Children.Add(new DefaultViewModel(s, this));
            break;
          case "DefaultFlags":
            DefaultFlags f = new DefaultFlags();
            ((Defaults)Model).Items.Add(f);
            this.Children.Add(new DefaultViewModel(f, this));
            break;
          case "DefaultBlob":
            DefaultBlob b = new DefaultBlob();
            ((Defaults)Model).Items.Add(b);
            this.Children.Add(new DefaultViewModel(b, this));
            break;
        }
      }
    }

    protected override void DeleteChild(ViewModelBase viewModel)
    {
      ((Defaults)Model).Items.Remove(viewModel.Model);
      this.Children.Remove(viewModel);
    }

    private bool HasDefault(string type)
    {
      var d = (from object o in ((Defaults)Model).Items where o.GetType().Name == type select o).FirstOrDefault();
      return (d != null);
    }
  }

  class DefaultViewModel : ViewModelBase
  {
    public DefaultViewModel(Default model, ViewModelBase parent)
      :base(model, parent)
    {
    }
  }
}
