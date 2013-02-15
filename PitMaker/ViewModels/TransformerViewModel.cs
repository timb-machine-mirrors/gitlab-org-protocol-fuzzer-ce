using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PitMaker.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace PitMaker.ViewModels
{
  class TransformerViewModel : ViewModelBase
  {
    public TransformerViewModel(Transformer model, ViewModelBase parent)
      :base(model, parent)
    {
      if (model.PeachType == null)
      {
        model.PeachType = (from Type t in this.TransformerTypes where t.Name == model.TransformerClass select t).FirstOrDefault();
        if (model.PeachType == null)
        {
          throw new Peach.Core.PeachException("Transformer class '" + model.TransformerClass + "' can not be found in loaded assemblies.");
        }
      }

      TransformerTypeCommands = new ObservableCollection<CreateTypeCommand>();
      foreach (Type t in TransformerTypes)
      {
        CreateTypeCommand c = new CreateTypeCommand(t, this.CreateChild);
        TransformerTypeCommands.Add(c);
      }
      RaisePropertyChanged("TransformerTypeCommands");
    }

    [Browsable(false)]
    public override string Image
    {
      get
      {
        return "Images/node-transform.png";
      }
    }

    internal override void ExecuteCreateChild(string parameter)
    {
      Type transformerType = (from t in this.TransformerTypes where t.FullName == parameter select t as Type).FirstOrDefault();
      if (transformerType != null)
      {
        Transformer transformer = new Transformer(transformerType);
        ((Transformer)Model).Items.Add(transformer);
        TransformerViewModel tvm = new TransformerViewModel(transformer, this);
        this.Children.Add(tvm);
        return;
      }
    }

    protected override void DeleteChild(ViewModelBase viewModel)
    {
      ((Models.Transformer)Model).Items.Remove((Models.Transformer)viewModel.Model);
      this.Children.Remove(viewModel);
    }

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
  }
}
