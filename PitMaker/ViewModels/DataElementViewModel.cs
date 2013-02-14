using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using PitMaker.Models;
using System.ComponentModel;


namespace PitMaker.ViewModels
{
  class DataElementViewModel : ViewModelBase
  {
    public DataElementViewModel(Models.DataElement dataElement, ViewModelBase parent)
      :base(dataElement, parent)
    {
      //this.PeachViewModel = peachViewModel;
      this.PeachViewModel.DataModelNodes.Add(dataElement);

      FixupTypeCommands = new ObservableCollection<CreateTypeCommand>();
      foreach (Type t in FixupTypes)
      {
        CreateTypeCommand c = new CreateTypeCommand(t, this.CreateChild);
        FixupTypeCommands.Add(c);
      }
      RaisePropertyChanged("FixupTypeCommands");

      TransformerTypeCommands = new ObservableCollection<CreateTypeCommand>();
      foreach (Type t in TransformerTypes)
      {
        CreateTypeCommand c = new CreateTypeCommand(t, this.CreateChild);
        TransformerTypeCommands.Add(c);
      }
      RaisePropertyChanged("TransformerTypeCommands");

      AnalyzerTypeCommands = new ObservableCollection<CreateTypeCommand>();
      foreach (Type t in AnalyzerTypes)
      {
        AnalyzerTypeCommands.Add(new CreateTypeCommand(t, this.CreateChild));
      }
      RaisePropertyChanged("AnalyzerTypeCommands");

      this.Image = String.Format("Images/node-{0}.png", dataElement.PeachType.Name.ToLower());

      this.Children = WrapItems();
    }

    protected ObservableCollection<ViewModelBase> WrapItems()
    {
      ObservableCollection<ViewModelBase> vms = new ObservableCollection<ViewModelBase>();

      Models.DataElement dataModel = (Models.DataElement)this.Model;

      var analyzers = from object n in dataModel.Items where n is Analyzer select n as Analyzer;
      foreach (var model in analyzers)
      {
        vms.Add(new AnalyzerViewModel(model, this));
      }

      var fixups = from object n in dataModel.Items where n is Fixup select n as Fixup;
      foreach (var model in fixups)
      {
        vms.Add(new FixupViewModel(model, this));
      }

      var hints = from object n in dataModel.Items where n is Hint select n as Hint;
      foreach (var model in hints)
      {
        vms.Add(new HintViewModel(model, this));
      }

      var placements = from object n in dataModel.Items where n is Placement select n as Placement;
      foreach (var model in placements)
      {
        vms.Add(new PlacementViewModel(model, this));
      }

      var relations = from object n in dataModel.Items where n is Relation select n as Relation;
      foreach (var model in relations)
      {
        vms.Add(new RelationViewModel(model, this));
      }

      var transformers = from object n in dataModel.Items where n is Transformer select n as Transformer;
      foreach (var model in transformers)
      {
        vms.Add(new TransformerViewModel(model, this));
      }

      return vms;
    }

    internal override void ExecuteCreateChild(string parameter)
    {
      Type fixupType = (from t in this.FixupTypes where t.FullName == parameter select t as Type).FirstOrDefault();
      if (fixupType != null)
      {
        Fixup fixup = new Fixup(fixupType);
        ((DataElement)Model).Items.Add(fixup);
        FixupViewModel fvm = new FixupViewModel(fixup, this);
        this.Children.Add(fvm);
        return;
      }

      Type analyzerType = (from t in this.AnalyzerTypes where t.FullName == parameter select t as Type).FirstOrDefault();
      if (analyzerType != null)
      {
        Analyzer analyzer = new Analyzer(analyzerType);
        ((DataElement)Model).Items.Add(analyzer);
        AnalyzerViewModel avm = new AnalyzerViewModel(analyzer, this);
        this.Children.Add(avm);
        return;
      }

      Type transformerType = (from t in this.TransformerTypes where t.FullName == parameter select t as Type).FirstOrDefault();
      if (transformerType != null)
      {
        Transformer transformer = new Transformer(transformerType);
        ((DataElement)Model).Items.Add(transformer);
        TransformerViewModel tvm = new TransformerViewModel(transformer, this);
        this.Children.Add(tvm);
        return;
      }

      Node m = null;
      ViewModelBase vm = null;
      switch (parameter)
      {
        case "Hint":
          m = new Hint();
          vm = new HintViewModel((Hint)m, this);
          break;
        case "Placement":
          m = new Placement();
          vm = new PlacementViewModel((Placement)m, this);
          break;
        case "RelationCount":
          m = new CountRelation();
          vm = new CountRelationViewModel((CountRelation)m, this);
          break;
        case "RelationSize":
          m = new SizeRelation();
          vm = new SizeRelationViewModel((SizeRelation)m, this);
          break;
        case "RelationOffset":
          m = new OffsetRelation();
          vm = new OffsetRelationViewModel((OffsetRelation)m, this);
          break;
        default:
          throw new Peach.Core.PeachException("Data Element child type not supported: " + parameter);
      }

      if (m != null)
      {
        ((DataElement)this.Model).Items.Add(m);
        this.Children.Add(vm);
      }
    }

    protected override void DeleteChild(ViewModelBase viewModel)
    {
      ((DataElement)Model).Items.Remove(viewModel.Model);
      this.Children.Remove(viewModel);
    }

    #region FixupTypeCommands Property

    private ObservableCollection<CreateTypeCommand> fixupTypeCommandsField;

    [Browsable(false)]
    public ObservableCollection<CreateTypeCommand> FixupTypeCommands
    {
      get
      {
        return fixupTypeCommandsField;
      }
      set
      {
        if (fixupTypeCommandsField != value)
        {
          fixupTypeCommandsField = value;
          RaisePropertyChanged("FixupTypeCommands");
        }
      }
    }

    #endregion

    #region TransformerTypeCommands Property

    private ObservableCollection<CreateTypeCommand> transformerTypeCommands;

    [Browsable(false)]
    public ObservableCollection<CreateTypeCommand> TransformerTypeCommands
    {
      get
      {
        return this.transformerTypeCommands;
      }
      set
      {
        if (this.transformerTypeCommands != value)
        {
          this.transformerTypeCommands = value;
          RaisePropertyChanged("TransformerTypeCommands");
        }
      }
    }

    #endregion

    #region AnalyzerTypeCommands Property
    private ObservableCollection<CreateTypeCommand> analyzerTypeCommandsField;

    [Browsable(false)]
    public ObservableCollection<CreateTypeCommand> AnalyzerTypeCommands
    {
      get
      {
        return analyzerTypeCommandsField;
      }
      set
      {
        if (analyzerTypeCommandsField != value)
        {
          analyzerTypeCommandsField = value;
          RaisePropertyChanged("AnalyzerTypeCommands");
        }
      }
    }
    #endregion


  }
}
