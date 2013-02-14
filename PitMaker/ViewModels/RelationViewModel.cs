using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PitMaker.Models;
using System.ComponentModel;

namespace PitMaker.ViewModels
{
  class RelationViewModel : ViewModelBase
  {
    public RelationViewModel(Relation model, ViewModelBase parent)
      :base(model, parent)
    {
      this.DisplayName = model.GetType().Name;
    }

    [Browsable(false)]
    public override string Image
    {
      get
      {
        return "Images/node-relation.png";
      }
    }

    #region DisplayName Property

    private string displayNameField;

    [Browsable(false)]
    public string DisplayName
    {
      get
      {
        return this.displayNameField;
      }
      set
      {
        if (this.displayNameField != value)
        {
          this.displayNameField = value;
          RaisePropertyChanged("DisplayName");
        }
      }
    }

    #endregion


  }

  class CountRelationViewModel : RelationViewModel
  {
    public CountRelationViewModel(CountRelation model, ViewModelBase parent)
      :base(model, parent)
    {
    }
  }

  class SizeRelationViewModel : RelationViewModel
  {
    public SizeRelationViewModel(SizeRelation model, ViewModelBase parent)
      : base(model, parent)
    {
    }
  }

  class OffsetRelationViewModel : RelationViewModel
  {
    public OffsetRelationViewModel(OffsetRelation model, ViewModelBase parent)
      : base(model, parent)
    {
    }
  }
}
