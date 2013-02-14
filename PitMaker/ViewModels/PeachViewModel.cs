using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Models=PitMaker.Models;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.ComponentModel;
using Microsoft.Practices.Prism.Commands;
using PitMaker.Models;
using System.Xml.Serialization;
using Microsoft.Win32;
using System.IO;
using System.Diagnostics;

namespace PitMaker.ViewModels
{
  class PeachViewModel : ViewModelBase
  {
    IncludesAssociation ina;
    RequiresAssociation ra;
    ImportsAssociation ima;
    PythonPathsAssociation ppa;
    RubyPathsAssociation rpa;
    PythonsAssociation pya;
    RubysAssociation rua;
    DefaultsViewModel dvm;
    AgentsAssociation aga;
    DataModelsAssociation dma;
    StateModelsAssociation sma;
    TestsAssociation ta;

    public PeachViewModel(Models.PeachPitDocument model, ViewModelBase parent)
      :base(model, parent)
    {
      Debug.WriteLine("PeachViewModel: " + this.GetHashCode());

      if (dataModelNodesField.Count > 0)
      {
        dataModelNodesField.Clear();
      }

      this.PropertyChanged += new PropertyChangedEventHandler(PeachViewModel_PropertyChanged);

      this.Children = WrapItems();

      this.IsExpanded = true;


      IsDirty = false;

    }

    [Browsable(false)]
    public override string Image
    {
      get
      {
        return "Images/node-peach.png";
      }
    }

    void PeachViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
    }

    #region MessageRaised

    public event EventHandler<MessageRaisedEventArgs> MessageRaised;
    
    internal void RaiseMessage(string message)
    {
      if (MessageRaised != null)
      {
        MessageRaised(this, new MessageRaisedEventArgs(message));
      }
      else
      {
        Debug.WriteLine("no handler: " + this.GetHashCode());
      }

    }

    internal void RaiseMessage(string format, object arg0 = null, object arg1 = null, object arg2 = null)
    {
      RaiseMessage(String.Format(format, arg0, arg1, arg2));
    }

    public class MessageRaisedEventArgs : EventArgs
    {
      public MessageRaisedEventArgs(string message)
      {
        this.Message = message;
      }

      public string Message { get; set; }
    }

    #endregion

    #region WrapItems

    protected ObservableCollection<ViewModelBase> WrapItems()
    {
      ObservableCollection<ViewModelBase> vms = new ObservableCollection<ViewModelBase>();

      Models.PeachPitDocument model = this.Model as Models.PeachPitDocument;

      WrapIncludes(from i in model.Items where i is Models.Include select i as Include, vms);

      WrapRequires(from i in model.Items where i is Models.Require select i as Require, vms);

      WrapImports(from i in model.Items where i is Models.Import select i as Import, vms);

      WrapPythonPaths(from i in model.Items where i is Models.PythonPath select i as PythonPath, vms);

      WrapRubyPaths(from i in model.Items where i is Models.RubyPath select i as RubyPath, vms);

      WrapPythons(from i in model.Items where i is Models.Python select i as Python, vms);

      WrapRubys(from i in model.Items where i is Models.Ruby select i as Ruby, vms);

      WrapDefaults((from i in model.Items where i is Models.Defaults select i as Defaults).FirstOrDefault(), vms);

      WrapAgents(from i in model.Items where i is Models.Agent select i as Models.Agent, vms);

      WrapDataModels(from i in model.Items where i is Models.DataModel select i as Models.DataModel, vms);

      WrapStateModels(from i in model.Items where i is Models.StateModel select i as Models.StateModel, vms);

      WrapTests(from i in model.Items where i is Models.Test select i as Models.Test, vms);

      return vms;
    }

    private void WrapDefaults(Defaults defaults, ObservableCollection<ViewModelBase> vms)
    {
      if (defaults != null)
      {
        if (dvm == null)
        {
          dvm = new DefaultsViewModel(defaults, this);
          vms.Add(dvm);
        }
      }
    }

    private void WrapRequires(IEnumerable<Require> iEnumerable, ObservableCollection<ViewModelBase> vms)
    {
      if (iEnumerable.Count() > 0)
      {
        if (ra == null)
        {
          ra = new RequiresAssociation(this);
          vms.Add(ra);
        }
        foreach (var model in iEnumerable)
        {
          ra.Add(new RequireViewModel(model, this));
        }
      }
    }

    private void WrapRubys(IEnumerable<Ruby> iEnumerable, ObservableCollection<ViewModelBase> vms)
    {
      if (iEnumerable.Count() > 0)
      {
        if (rua == null)
        {
          rua = new RubysAssociation(this);
          vms.Add(rua);
        }

        foreach (var model in iEnumerable)
        {
          rua.Add(new RubyViewModel(model, this));
        }
      }
    }

    private void WrapRubyPaths(IEnumerable<RubyPath> iEnumerable, ObservableCollection<ViewModelBase> vms)
    {
      if (iEnumerable.Count() > 0)
      {
        if (rpa == null)
        {
          rpa = new RubyPathsAssociation(this);
          vms.Add(rpa);
        }
        foreach (var model in iEnumerable)
        {
          rpa.Add(new RubyPathViewModel(model, this));
        }
      }
    }

    private void WrapAgents(IEnumerable<Models.Agent> iEnumerable, ObservableCollection<ViewModelBase> vms)
    {
      if (iEnumerable.Count() > 0)
      {
        if (aga == null)
        {
          aga = new AgentsAssociation(this);
          vms.Add(aga);
        }
        foreach (var model in iEnumerable)
        {
          aga.Add(new AgentViewModel(model, this));
        }
      }
    }

    private void WrapDataModels(IEnumerable<Models.DataModel> iEnumerable, ObservableCollection<ViewModelBase> vms)
    {
      if (iEnumerable.Count() > 0)
      {
        if (dma == null)
        {
          dma = new DataModelsAssociation(this);
          vms.Add(dma);
        }
        foreach (var model in iEnumerable)
        {
          dma.Add(new DataModelViewModel(model, this));
        }
      }
    }

    private void WrapImports(IEnumerable<Models.Import> iEnumerable, ObservableCollection<ViewModelBase> vms)
    {
      if (iEnumerable.Count() > 0)
      {
        if (ima == null)
        {
          ima = new ImportsAssociation(this);
          vms.Add(ima);
        }
        foreach (var model in iEnumerable)
        {
          ima.Add(new ImportViewModel(model, this));
        }
      }
    }

    private void WrapIncludes(IEnumerable<Models.Include> iEnumerable, ObservableCollection<ViewModelBase> vms)
    {
      if (iEnumerable.Count() > 0)
      {
        if (ina == null)
        {
          ina = new IncludesAssociation(this);
          vms.Add(ina);
        }
        foreach (var model in iEnumerable)
        {
          ina.Add(new IncludeViewModel(model, this));
        }
      }
    }

    private void WrapPythons(IEnumerable<Models.Python> iEnumerable, ObservableCollection<ViewModelBase> vms)
    {
      if (iEnumerable.Count() > 0)
      {
        if (pya == null)
        {
          pya = new PythonsAssociation(this);
          vms.Add(pya);
        }
        foreach (var model in iEnumerable)
        {
          pya.Add(new PythonViewModel(model, this));
        }
      }
    }

    private void WrapPythonPaths(IEnumerable<Models.PythonPath> iEnumerable, ObservableCollection<ViewModelBase> vms)
    {
      if (iEnumerable.Count() > 0)
      {
        if (ppa == null)
        {
          ppa = new PythonPathsAssociation(this);
          vms.Add(ppa);
        }
        foreach (var model in iEnumerable)
        {
          ppa.Add(new PythonPathViewModel(model, this));
        }
      }
    }

    private void WrapStateModels(IEnumerable<Models.StateModel> iEnumerable, ObservableCollection<ViewModelBase> vms)
    {
      if (iEnumerable.Count() > 0)
      {
        if (sma == null)
        {
          sma = new StateModelsAssociation(this);
          vms.Add(sma);
        }
        foreach (var model in iEnumerable)
        {
          sma.Add(new StateModelViewModel(model, this));
        }
      }
    }

    private void WrapTests(IEnumerable<Models.Test> iEnumerable, ObservableCollection<ViewModelBase> vms)
    {
      if (iEnumerable.Count() > 0)
      {
        if (ta == null)
        {
          ta = new TestsAssociation(this);
          vms.Add(ta);
        }
        foreach (var model in iEnumerable)
        {
          ta.Add(new TestViewModel(model, this));
        }
      }
    }

    #endregion

    #region Overrides

    internal override void ExecuteCreateChild(string parameter)
    {

      PeachPitDocument peach = ((PeachPitDocument)this.Model);
      Node m = null;
      switch (parameter)
      {
        case "Include":
          if (ina == null)
          {
            ina = new IncludesAssociation(this);
            this.Children.Add(ina);
          }
          m = new Include();
          peach.Items.Add(m);
          this.ina.Add(new IncludeViewModel((Include)m, this));
          break;
        case "Import":
          if (ima == null)
          {
            ima = new ImportsAssociation(this);
            this.Children.Add(ima);
          }
          m = new Import();
          peach.Items.Add(m);
          this.ima.Add(new ImportViewModel((Import)m, this));
          break;
        case "Require":
          if (ra == null)
          {
            ra = new RequiresAssociation(this);
            this.Children.Add(ra);
          }
          m = new Require();
          peach.Items.Add(m);
          this.ra.Add(new RequireViewModel((Require)m, this));
          break;
        case "Python":
          if (peach.HasRuby == false)
          {
            if (pya == null)
            {
              pya = new PythonsAssociation(this);
              this.Children.Add(pya);
            }
            m = new Python();
            peach.Items.Add(m);
            this.pya.Add(new PythonViewModel((Python)m, this));
          }
          else
          {
            throw new Peach.Core.PeachException("Can not mix Ruby and Python.");
          }
          break;
        case "PythonPath":
          if (peach.HasRuby == false)
          {
            if (ppa == null)
            {
              ppa = new PythonPathsAssociation(this);
              this.Children.Add(ppa);
            }
            m = new PythonPath();
            peach.Items.Add(m);
            this.ppa.Add(new PythonPathViewModel((PythonPath)m, this));
          }
          else
          {
            throw new Peach.Core.PeachException("Can not mix Ruby and Python.");
          }
          break;
        case "Ruby":
          if (peach.HasPython == false)
          {
            if (rua == null)
            {
              rua = new RubysAssociation(this);
              this.Children.Add(rua);
            }
            m = new Ruby();
            peach.Items.Add(m);
            this.rua.Add(new RubyViewModel((Ruby)m, this));
          }
          else
          {
            throw new Peach.Core.PeachException("Can not mix Ruby and Python.");
          }
          break;
        case "RubyPath":
          if (peach.HasPython == false)
          {
            if (rpa == null)
            {
              rpa = new RubyPathsAssociation(this);
              this.Children.Add(rpa);
            }
            m = new RubyPath();
            peach.Items.Add(m);
            this.rpa.Add(new RubyPathViewModel((RubyPath)m, this));
          }
          else
          {
            throw new Peach.Core.PeachException("Can not mix Ruby and Python.");
          }
          break;
        case "Agent":
          if (aga == null)
          {
            aga = new AgentsAssociation(this);
            this.Children.Add(aga);
          }
          m = new Agent();
          peach.Items.Add(m);
          this.aga.Add(new AgentViewModel((Agent)m, this));
          break;
        case "StateModel":
          if (sma == null)
          {
            sma = new StateModelsAssociation(this);
            this.Children.Add(sma);
          }
          m = new StateModel();
          peach.Items.Add(m);
          this.sma.Add(new StateModelViewModel((StateModel)m, this));
          break;
        case "DataModel":
          if (dma == null)
          {
            dma = new DataModelsAssociation(this);
            this.Children.Add(dma);
          }
          m = new DataModel();
          peach.Items.Add(m);
          this.dma.Add(new DataModelViewModel((DataModel)m, this));
          break;
        case "Test":
          if (ta == null)
          {
            ta = new TestsAssociation(this);
            this.Children.Add(ta);
          }
          m = new Test();
          peach.Items.Add(m);
          this.ta.Add(new TestViewModel((Test)m, this));
          break;
        case "DefaultNumber":
        case "DefaultString":
        case "DefaultFlags":
        case "DefaultBlob":
          if (dvm == null)
          {
            Defaults model = new Defaults();
            peach.Items.Add(model);

            dvm = new DefaultsViewModel(model, this);
            this.Children.Add(dvm);
          }
          dvm.ExecuteCreateChild(parameter);
          break;
        default:
          throw new Peach.Core.PeachException("Type not recognized: " + parameter);
      }
    }

    protected override void DeleteChild(ViewModelBase viewModel)
    {
      ((PeachPitDocument)this.Model).Items.Remove(viewModel.Model);

      if (viewModel is AgentViewModel)
      {
        this.aga.Remove(viewModel);
      }
      else if (viewModel is DataModelViewModel)
      {
        this.DataModelNodes.Remove(viewModel.Model);
        this.dma.Remove(viewModel);
      }
      else if (viewModel is StateModelViewModel)
      {
        this.sma.Remove(viewModel);
      }
      else if (viewModel is TestViewModel)
      {
        this.ta.Remove(viewModel);
      }
      else if (viewModel is RequireViewModel)
      {
        this.ra.Remove(viewModel);
      }
      else if (viewModel is PythonViewModel)
      {
        this.pya.Remove(viewModel);
      }
      else if (viewModel is PythonPathViewModel)
      {
        this.ppa.Remove(viewModel);
      }
      else if (viewModel is RubyViewModel)
      {
        this.rua.Remove(viewModel);
      }
      else if (viewModel is RubyPathViewModel)
      {
        this.rpa.Remove(viewModel);
      }
      else if (viewModel is ImportViewModel)
      {
        this.ima.Remove(viewModel);
      }
      else if (viewModel is IncludeViewModel)
      {
        this.ina.Remove(viewModel);
      }
      else
      {
        throw new NotImplementedException();
      }


    }

    #endregion

    #region Properties

    [Browsable(false)]
    public ObservableCollection<StateModel> StateModels
    {
      get
      {
        return new ObservableCollection<StateModel>(from StateModelViewModel smvm in this.sma.Children select smvm.Model as StateModel);
      }
    }

    [Browsable(false)]
    public ObservableCollection<DataModel> DataModels
    {
      get
      {
        if (dma == null)
          return new ObservableCollection<DataModel>();
        else
          return new ObservableCollection<DataModel>(from DataModelViewModel dmvm in this.dma.Children select dmvm.Model as DataModel);
      }
    }

    [Browsable(false)]
    public ObservableCollection<Agent> Agents
    {
      get
      {
        return new ObservableCollection<Agent>(from AgentViewModel avm in this.aga.Children select avm.Model as Agent);
      }
    }

    #region DataModelNodes Property

    private static ObservableCollection<Node> dataModelNodesField = new ObservableCollection<Node>();

    [Browsable(false)]
    public ObservableCollection<Node> DataModelNodes
    {
      get
      {
        return dataModelNodesField;
      }
      set
      {
        if (dataModelNodesField != value)
        {
          dataModelNodesField = value;
          RaisePropertyChanged("DataModelNodes");
        }
      }
    }

    #endregion





    #endregion
  }
}
