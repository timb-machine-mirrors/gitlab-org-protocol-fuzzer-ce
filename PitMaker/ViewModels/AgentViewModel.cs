using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PitMaker.Models;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace PitMaker.ViewModels
{
  class AgentViewModel : ViewModelBase
  {
    public AgentViewModel(Agent model, ViewModelBase parent)
      :base(model, parent)
    {
      if (monitorTypeCommands == null)
      {
        monitorTypeCommands = new ObservableCollection<CreateTypeCommand>();
        foreach (Type t in MonitorTypes)
        {
          monitorTypeCommands.Add(new CreateTypeCommand(t, CreateChild));
        }
      }

      this.Children = WrapItems();
    }

    protected ObservableCollection<ViewModelBase> WrapItems()
    {
      ObservableCollection<ViewModelBase> vms = new ObservableCollection<ViewModelBase>();
      Agent model = (Agent)this.Model;

      var monitors = from NodeWithParameters n in model.Items where n is Monitor select n as Monitor;
      foreach (var monitor in monitors)
      {
        vms.Add(new MonitorViewModel(monitor, this));
      }


      return vms;
    }

    [Browsable(false)]
    public override string Image
    {
      get
      {
        return "Images/node-agent.png";
      }
    }

    internal override void ExecuteCreateChild(string parameter)
    {
      Type monitorType = (from Type t in MonitorTypes where t.FullName == parameter select t as Type).FirstOrDefault();
      if (monitorType != null)
      {
        Monitor model = new Monitor(monitorType);
        ((Agent)Model).Items.Add(model);

        MonitorViewModel viewModel = new MonitorViewModel(model, this);
        this.Children.Add(viewModel);
        return;
      }

      switch (parameter)
      {
        default:
          throw new Peach.Core.PeachException("Unknown node type: " + parameter);
      }
    }

    protected override void DeleteChild(ViewModelBase viewModel)
    {
      ((Agent)this.Model).Items.Remove(viewModel.Model);
      this.Children.Remove(viewModel);
    }

    #region MonitorTypeCommands Property

    private ObservableCollection<CreateTypeCommand> monitorTypeCommands;

    [Browsable(false)]
    public ObservableCollection<CreateTypeCommand> MonitorTypeCommands
    {
      get
      {
        return monitorTypeCommands;
      }
      set
      {
        if (monitorTypeCommands != value)
        {
          monitorTypeCommands = value;
          RaisePropertyChanged("MonitorTypeCommands");
        }
      }
    }

    #endregion


  }
}
