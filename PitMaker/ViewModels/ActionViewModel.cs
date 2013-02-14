using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PitMaker.Models;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace PitMaker.ViewModels
{
  class ActionViewModel : ViewModelBase
  {
    public ActionViewModel(Models.Action model, ViewModelBase parent)
      :base(model, parent)
    {
    }

    [Browsable(false)]
    public override string Image
    {
      get
      {
        return "Images/node-action.png";
      }
    }

    public static ActionViewModel Create(Models.Action model, ViewModelBase parent)
    {
      switch (model.Type)
      {
        case ActionType.call:
          return new CallActionViewModel(model, parent);
        case ActionType.input:
          {
            var vm = new InputActionViewModel(model, parent);
            if (model.Items.Count == 0)
            {
              DataModelReferenceNode dmr = new DataModelReferenceNode();
              model.Items.Add(dmr);
              vm.Children.Add(new ReferenceViewModel(dmr, parent));
            }
            return vm;
          }
        case ActionType.output:
          {
            var vm = new OutputActionViewModel(model, parent);
            if (model.Items.Count == 0)
            {
              DataModelReferenceNode dmr = new DataModelReferenceNode();
              model.Items.Add(dmr);
              vm.Children.Add(new ReferenceViewModel(dmr, parent));

              Data d = new Data();
              model.Items.Add(d);
              vm.Children.Add(new DataViewModel(d, parent));
            }
            return vm;
          }
        case ActionType.getprop:
          {
            var vm = new GetPropActionViewModel(model, parent);
            if (model.Items.Count == 0)
            {
              DataModelReferenceNode dmr = new DataModelReferenceNode();
              model.Items.Add(dmr);
              vm.Children.Add(new ReferenceViewModel(dmr, parent));
            }
            return vm;
          }
        case ActionType.setprop:
          {
            var vm = new SetPropActionViewModel(model, parent);
            if (model.Items.Count == 0)
            {
              DataModelReferenceNode dmr = new DataModelReferenceNode();
              model.Items.Add(dmr);
              vm.Children.Add(new ReferenceViewModel(dmr, parent));

              Data d = new Data();
              model.Items.Add(d);
              vm.Children.Add(new DataViewModel(d, parent));
            }
            return vm;
          }
        case ActionType.slurp:
          return new SlurpActionViewModel(model, parent);
        case ActionType.changeState:
          return new ChangeStateActionViewModel(model, parent);
        default:
          return new ActionViewModel(model, parent);
      }

    }

    #region DisplayName Property

    [Browsable(false)]
    public string DisplayName
    {
      get
      {
        return ((Models.Action)Model).Type.ToString();
      }
    }

    #endregion
  }

  class CallActionViewModel : ActionViewModel
  {
    public CallActionViewModel(Models.Action model, ViewModelBase parent)
      :base(model, parent)
    {
      this.Children = WrapItems();
    }

    protected ObservableCollection<ViewModelBase> WrapItems()
    {
      ObservableCollection<ViewModelBase> vms = new ObservableCollection<ViewModelBase>();
      if (((Models.Action)Model).Items.Count > 0)
      {
        foreach (object item in ((Models.Action)Model).Items)
        {
          ViewModelBase vm;
          if (item is Models.ActionParam)
          {
            vm = new ActionParamViewModel((Models.ActionParam)item, this);
          }
          else
          {
            throw new Peach.Core.PeachException("Only Param elements are valid children of Actions.");
          }

          vms.Add(vm);
        }
      }
      return vms;
    }

    #region Method Property

    [Category(Categories.Required)]
    public string Method
    {
      get
      {
        return ((Models.Action)this.Model).Method;
      }
      set
      {
        if (((Models.Action)this.Model).Method != value)
        {
          ((Models.Action)this.Model).Method = value;
          RaisePropertyChanged("Method");
        }
      }
    }

    #endregion

    #region Items Property
    [Browsable(false)]
    public ObservableCollection<object> Items
    {
      get
      {
        return ((Models.Action)this.Model).Items;
      }
      set
      {
        if (((Models.Action)this.Model).Items != value)
        {
          ((Models.Action)this.Model).Items = value;
          RaisePropertyChanged("Items");
        }
      }
    }

    #endregion

    internal override void ExecuteCreateChild(string parameter)
    {
      switch (parameter)
      {
        case "ActionParam":
          ActionParam ap = new ActionParam();
          
          DataModelReferenceNode dmr = new DataModelReferenceNode();
          ap.Items.Add(dmr);

          Data d = new Data();
          ap.Items.Add(d);

          ((Models.Action)this.Model).Items.Add(ap);
          ActionParamViewModel vm = new ActionParamViewModel(ap, this);
          this.Children.Add(vm);
          break;
      }
    }

    protected override void DeleteChild(ViewModelBase viewModel)
    {
      ((Models.Action)this.Model).Items.Remove(viewModel.Model);
      this.Children.Remove(viewModel);
    }

  }

  class InputActionViewModel : ActionViewModel
  {
    public InputActionViewModel(Models.Action model, ViewModelBase parent)
      : base(model, parent)
    {
      this.Children = WrapItems();
    }

    protected ObservableCollection<ViewModelBase> WrapItems()
    {
      ObservableCollection<ViewModelBase> vms = new ObservableCollection<ViewModelBase>();
      if (((Models.Action)Model).Items.Count > 0)
      {
        foreach (Models.Node item in ((Models.Action)Model).Items)
        {
          ViewModelBase vm;
          if (item is Models.DataModelReferenceNode)
          {
            vm = new ReferenceViewModel((Models.DataModelReferenceNode)item, this);
          }
          else
          {
            throw new Peach.Core.PeachException("Actions of type \"input\" can only have Data Models as children");
          }

          vms.Add(vm);
        }
      }
      return vms;
    }

    #region Items Property
    [Browsable(false)]
    public ObservableCollection<object> Items
    {
      get
      {
        return ((Models.Action)this.Model).Items;
      }
      set
      {
        if (((Models.Action)this.Model).Items != value)
        {
          ((Models.Action)this.Model).Items = value;
          RaisePropertyChanged("Items");
        }
      }
    }

    #endregion

    internal override void ExecuteCreateChild(string parameter)
    {
      switch (parameter)
      {
        case "DataModelReference":
          DataModelReferenceNode dmr = new DataModelReferenceNode();
          ((Models.Action)this.Model).Items.Add(dmr);
          this.Children.Add(new ReferenceViewModel(dmr, this));
          break;
      }
    }

    protected override void DeleteChild(ViewModelBase viewModel)
    {
      ((Models.Action)this.Model).Items.Remove(viewModel.Model);
      this.Children.Remove(viewModel);
    }

  }

  class OutputActionViewModel : ActionViewModel
  {
    public OutputActionViewModel(Models.Action model, ViewModelBase parent)
      : base(model, parent)
    {
      this.Children = WrapItems();
    }

    protected ObservableCollection<ViewModelBase> WrapItems()
    {
      ObservableCollection<ViewModelBase> vms = new ObservableCollection<ViewModelBase>();
      if (((Models.Action)this.Model).Items.Count > 0)
      {
        foreach (Models.Node item in ((Models.Action)this.Model).Items)
        {
          ViewModelBase vm;
          if (item is Models.DataModelReferenceNode)
          {
            vm = new ReferenceViewModel((Models.DataModelReferenceNode)item, this);
          }
          else if (item is Models.Data)
          {
            vm = new DataViewModel((Models.Data)item, this);
          }
          else
          {
            throw new Peach.Core.PeachException("Actions of type \"output\" can only have one Data Model and one Data element as children");
          }

          vms.Add(vm);
        }
      }
      return vms;
    }

    #region Items Property
    [Browsable(false)]
    public ObservableCollection<object> Items
    {
      get
      {
        return ((Models.Action)this.Model).Items;
      }
      set
      {
        if (((Models.Action)this.Model).Items != value)
        {
          ((Models.Action)this.Model).Items = value;
          RaisePropertyChanged("Items");
        }
      }
    }

    #endregion

    internal override void ExecuteCreateChild(string parameter)
    {
      switch (parameter)
      {
        case "Data":
          Data d = new Data();
          ((Models.Action)this.Model).Items.Add(d);
          this.Children.Add(new DataViewModel(d, this));
          break;
        case "DataModelReference":
          DataModelReferenceNode dmr = new DataModelReferenceNode();
          ((Models.Action)this.Model).Items.Add(dmr);
          this.Children.Add(new ReferenceViewModel(dmr, this));
          break;
      }
    }

    protected override void DeleteChild(ViewModelBase viewModel)
    {
      ((Models.Action)this.Model).Items.Remove(viewModel.Model);
      this.Children.Remove(viewModel);
    }

  }

  class GetPropActionViewModel : ActionViewModel
  {
    public GetPropActionViewModel(Models.Action model, ViewModelBase parent)
      : base(model, parent)
    {
      this.Children = WrapItems();
    }

    protected ObservableCollection<ViewModelBase> WrapItems()
    {
      ObservableCollection<ViewModelBase> vms = new ObservableCollection<ViewModelBase>();
      if (((Models.Action)Model).Items.Count > 0)
      {
        foreach (Models.Node item in ((Models.Action)Model).Items)
        {
          ViewModelBase vm;
          if (item is Models.DataModelReferenceNode)
          {
            vm = new ReferenceViewModel((Models.DataModelReferenceNode)item, this);
          }
          else
          {
            throw new Peach.Core.PeachException("Actions of type \"getprop\" can only a Data Model as a child");
          }

          vms.Add(vm);
        }
      }
      return vms;
    }

    #region Property Property


    [Category(Categories.Required)]
    public string Property
    {
      get
      {
        return ((Models.Action)this.Model).Property;
      }
      set
      {
        if (((Models.Action)this.Model).Property != value)
        {
          ((Models.Action)this.Model).Property = value;
          RaisePropertyChanged("Property");
        }
      }
    }

    #endregion

    #region Items Property
    [Browsable(false)]
    public ObservableCollection<object> Items
    {
      get
      {
        return ((Models.Action)this.Model).Items;
      }
      set
      {
        if (((Models.Action)this.Model).Items != value)
        {
          ((Models.Action)this.Model).Items = value;
          RaisePropertyChanged("Items");
        }
      }
    }

    #endregion

    internal override void ExecuteCreateChild(string parameter)
    {
      switch (parameter)
      {
        case "DataModelReference":
          DataModelReferenceNode dmr = new DataModelReferenceNode();
          ((Models.Action)this.Model).Items.Add(dmr);
          this.Children.Add(new ReferenceViewModel(dmr, this));
          break;
      }
    }

    protected override void DeleteChild(ViewModelBase viewModel)
    {
      ((Models.Action)this.Model).Items.Remove(viewModel.Model);
      this.Children.Remove(viewModel);
    }

  }

  class SetPropActionViewModel : ActionViewModel
  {
    public SetPropActionViewModel(Models.Action model, ViewModelBase parent)
      : base(model, parent)
    {
      this.Children = WrapItems();
    }

    protected ObservableCollection<ViewModelBase> WrapItems()
    {
      ObservableCollection<ViewModelBase> vms = new ObservableCollection<ViewModelBase>();
      if (((Models.Action)Model).Items.Count > 0)
      {
        foreach (Models.Node item in ((Models.Action)Model).Items)
        {
          ViewModelBase vm;
          if (item is Models.DataModelReferenceNode)
          {
            vm = new ReferenceViewModel((Models.DataModelReferenceNode)item, this);
          }
          else if (item is Models.Data)
          {
            vm = new DataViewModel((Models.Data)item, this);
          }
          else
          {
            throw new Peach.Core.PeachException("Actions of type \"setprop\" can only have one Data Model and one Data element as children");
          }

          vms.Add(vm);
        }
      }
      return vms;
    }

    #region Property Property

    [Category(Categories.Required)]
    public string Property
    {
      get
      {
        return ((Models.Action)this.Model).Property;
      }
      set
      {
        if (((Models.Action)this.Model).Property != value)
        {
          ((Models.Action)this.Model).Property = value;
          RaisePropertyChanged("Property");
        }
      }
    }

    #endregion

    #region Items Property
    [Browsable(false)]
    public ObservableCollection<object> Items
    {
      get
      {
        return ((Models.Action)this.Model).Items;
      }
      set
      {
        if (((Models.Action)this.Model).Items != value)
        {
          ((Models.Action)this.Model).Items = value;
          RaisePropertyChanged("Items");
        }
      }
    }

    #endregion

    internal override void ExecuteCreateChild(string parameter)
    {
      switch (parameter)
      {
        case "Data":
          Data d = new Data();
          ((Models.Action)this.Model).Items.Add(d);
          this.Children.Add(new DataViewModel(d, this));
          break;
        case "DataModelReference":
          DataModelReferenceNode dmr = new DataModelReferenceNode();
          ((Models.Action)this.Model).Items.Add(dmr);
          this.Children.Add(new ReferenceViewModel(dmr, this));
          break;
      }
    }

    protected override void DeleteChild(ViewModelBase viewModel)
    {
      ((Models.Action)this.Model).Items.Remove(viewModel.Model);
      this.Children.Remove(viewModel);
    }

  }

  class SlurpActionViewModel : ActionViewModel
  {
    public SlurpActionViewModel(Models.Action model, ViewModelBase parent)
      :base(model, parent)
    {

    }

    #region SetXPath Property

    [Category(Categories.Required)]
    public string SetXPath
    {
      get
      {
        return ((Models.Action)this.Model).SetXPath;
      }
      set
      {
        if (((Models.Action)this.Model).SetXPath != value)
        {
          ((Models.Action)this.Model).SetXPath = value;
          RaisePropertyChanged("SetXPath");
        }
      }
    }

    #endregion

    #region ValueXPath Property

    [Category(Categories.Required)]
    public string ValueXPath
    {
      get
      {
        return ((Models.Action)this.Model).ValueXPath;
      }
      set
      {
        if (((Models.Action)this.Model).ValueXPath != value)
        {
          ((Models.Action)this.Model).ValueXPath = value;
          RaisePropertyChanged("GetXPath");
        }
      }
    }

    #endregion

  }

  class ChangeStateActionViewModel : ActionViewModel
  {
    public ChangeStateActionViewModel(Models.Action model, ViewModelBase parent)
      :base(model, parent)
    {

    }

    #region StateReference Property
    //*
    [Category(Categories.Required)]
    public string StateReference
    {
      get
      {
        return ((Models.Action)this.Model).Ref;
      }
      set
      {
        if (((Models.Action)this.Model).Ref != value)
        {
          ((Models.Action)this.Model).Ref = value;
          RaisePropertyChanged("StateReference");
        }
      }
    }
    //*/
    #endregion


  }

  class ActionParamViewModel : ViewModelBase
  {
    public ActionParamViewModel(ActionParam model, ViewModelBase parent)
      :base(model, parent)
    {
      this.Children = WrapItems();
    }

    //*
    protected ObservableCollection<ViewModelBase> WrapItems()
    {
      ObservableCollection<ViewModelBase> vms = new ObservableCollection<ViewModelBase>();
      ActionParam actionParam = ((Models.ActionParam)Model);
      if (actionParam.Items.Count > 0)
      {
        foreach (Models.Node item in actionParam.Items)
        {
          ViewModelBase vm;
          if (item is Models.DataModelReferenceNode)
          {
            vm = new ReferenceViewModel((Models.DataModelReferenceNode)item, this);
          }
          else if (item is Models.Data)
          {
            vm = new DataViewModel((Models.Data)item, this);
          }
          else
          {
            throw new Peach.Core.PeachException("Action parameters can only contain one Data Model and one Data element as children");
          }

          vms.Add(vm);
        }
      }
      return vms;
    }
    //*/
  }

}
