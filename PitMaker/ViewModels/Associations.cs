using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace PitMaker.ViewModels
{
  class Association : ViewModelBase
  {
    public Association(ViewModelBase parent)
      :base(null, parent)
    {
    }

    public void Add(ViewModelBase child)
    {
      if(child.Parent == null)
        child.Parent = this.Parent;

      if (this.Children == null)
        this.Children = new System.Collections.ObjectModel.ObservableCollection<ViewModelBase>();

      this.Children.Add(child);
      RaisePropertyChanged("Children");
    }

    public void Add(IEnumerable<ViewModelBase> children)
    {
      foreach (ViewModelBase child in children)
      {
        if(child.Parent == null)
          child.Parent = this.Parent;

        this.Children.Add(child);
      }

      RaisePropertyChanged("Children");
    }

    public void Remove(ViewModelBase child)
    {
      this.Children.Remove(child);
      RaisePropertyChanged("Children");
    }

    #region DisplayName Property

    private string displayNameField = "Children";

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

  class IncludesAssociation : Association
  {
    public IncludesAssociation(ViewModelBase parent)
      : base(parent)
    {
      this.DisplayName = "Includes";
    }
  }

  class RequiresAssociation : Association
  {
    public RequiresAssociation(ViewModelBase parent)
      : base(parent)
    {
      this.DisplayName = "Requires";
    }
  }

  class ImportsAssociation : Association
  {
    public ImportsAssociation(ViewModelBase parent)
      : base(parent)
    {
      this.DisplayName = "Imports";
    }

    [Browsable(false)]
    public override string Image
    {
      get
      {
        return "Images/node-import.png";
      }
    }

  }

  class PythonPathsAssociation : Association
  {
    public PythonPathsAssociation(ViewModelBase parent)
      : base(parent)
    {
      this.DisplayName = "Python Paths";
    }

    [Browsable(false)]
    public override string Image
    {
      get
      {
        return "Images/node-pythonpath.png";
      }
    }
  }

  class PythonsAssociation : Association
  {
    public PythonsAssociation(ViewModelBase parent)
      : base(parent)
    {
      this.DisplayName = "Pythons";
    }

  }

  class RubysAssociation : Association
  {
    public RubysAssociation(ViewModelBase parent)
      :base(parent)
    {
      this.DisplayName = "Rubys";
    }
  }

  class RubyPathsAssociation : Association
  {
    public RubyPathsAssociation(ViewModelBase parent)
      :base(parent)
    {
      this.DisplayName = "Ruby Paths";
    }
  }

  class DefaultsAssociation : Association
  {
    public DefaultsAssociation(ViewModelBase parent)
      :base(parent)
    {
      this.DisplayName = "Defaults";
    }
  }

  class DataAssociation : Association
  {
    public DataAssociation(ViewModelBase parent)
      : base(parent)
    {
      this.DisplayName = "Data";
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

  class AnalyzersAssociation : Association
  {
    public AnalyzersAssociation(ViewModelBase parent)
      :base(parent)
    {
      this.DisplayName = "Analyzers";
    }
  }

  class TestPublishersAssociation : Association
  {
    public TestPublishersAssociation(ViewModelBase parent)
      :base(parent)
    {
      this.DisplayName = "Publishers";
    }
  }

  class TestLoggersAssociation : Association
  {
    public TestLoggersAssociation(ViewModelBase parent)
      :base(parent)
    {
      this.DisplayName = "Loggers";
    }
  }

  class TestStrategiesAssociation : Association
  {
    public TestStrategiesAssociation(ViewModelBase parent)
      :base(parent)
    {
      this.DisplayName = "Strategies";
    }
  }

  class DataModelsAssociation : Association
  {
    public DataModelsAssociation(ViewModelBase parent)
      :base(parent)
    {
      this.DisplayName = "Data Models";
    }
  }

  class StateModelsAssociation : Association
  {
    public StateModelsAssociation(ViewModelBase parent)
      : base(parent)
    {
      this.DisplayName = "State Models";
    }

    [Browsable(false)]
    public override string Image
    {
      get
      {
        return "Images/node-statemachine.png";
      }
    }
  }

  class AgentsAssociation : Association
  {
    public AgentsAssociation(ViewModelBase parent)
      : base(parent)
    {
      this.DisplayName = "Agents";
    }

    [Browsable(false)]
    public override string Image
    {
      get
      {
        return "Images/node-agent.png";
      }
    }
  }

  class TestsAssociation : Association
  {
    public TestsAssociation(ViewModelBase parent)
      : base(parent)
    {
      this.DisplayName = "Tests";
    }

    [Browsable(false)]
    public override string Image
    {
      get
      {
        return "Images/node-test.png";
      }
    }
  }

  class MonitorsAssociation : Association
  {
    public MonitorsAssociation(ViewModelBase parent)
      : base(parent)
    {

    }

    [Browsable(false)]
    public override string Image
    {
      get
      {
        return "Images/node-monitor.png";
      }
    }
  }

  class ParametersAssociation : Association
  {
    public ParametersAssociation(ViewModelBase parent)
      : base(parent)
    {

    }
  }

  class StatesAssociation : Association
  {
    public StatesAssociation(ViewModelBase parent)
      : base(parent)
    {
    }
  }

  class TestIncludesExcludesAssociation : Association
  {
    public TestIncludesExcludesAssociation(ViewModelBase parent)
      :base(parent)
    {
      this.DisplayName = "Includes/Excludes";
    }
  }

}
