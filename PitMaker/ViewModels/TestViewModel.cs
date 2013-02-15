using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PitMaker.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace PitMaker.ViewModels
{
  class TestViewModel : ViewModelBase
  {
    TestIncludesExcludesAssociation tiea = null;
    TestPublishersAssociation tpa = null;
    TestLoggersAssociation tla = null;

    public TestViewModel(Test model, ViewModelBase parent)
      :base(model, parent)
    {
      if (loggerTypeCommands == null)
      {
        loggerTypeCommands = new ObservableCollection<CreateTypeCommand>();
        foreach (Type loggerType in this.LoggerTypes)
        {
          Peach.Core.LoggerAttribute la = (from object o in loggerType.GetCustomAttributes(typeof(Peach.Core.LoggerAttribute), false) where ((Peach.Core.LoggerAttribute)o).IsDefault select o as Peach.Core.LoggerAttribute).FirstOrDefault();
          if (la == null)
          {
            la = (from object o in loggerType.GetCustomAttributes(typeof(Peach.Core.LoggerAttribute), false) select o as Peach.Core.LoggerAttribute).FirstOrDefault();
          }
          loggerTypeCommands.Add(new CreateTypeCommand(la.Name, loggerType, this.CreateChild));
        }
      }

      if (publisherTypeCommands == null)
      {
        publisherTypeCommands = new ObservableCollection<CreateTypeCommand>();
        foreach (Type publisherType in this.PublisherTypes)
        {
          Peach.Core.PublisherAttribute pa = (from object o in publisherType.GetCustomAttributes(typeof(Peach.Core.PublisherAttribute), false) where ((Peach.Core.PublisherAttribute)o).IsDefault select o as Peach.Core.PublisherAttribute).FirstOrDefault();
          if (pa == null)
          {
            pa = (from object o in publisherType.GetCustomAttributes(typeof(Peach.Core.PublisherAttribute), false) select o as Peach.Core.PublisherAttribute).FirstOrDefault();
          }
          publisherTypeCommands.Add(new CreateTypeCommand(pa.Name, publisherType, this.CreateChild));
        }
      }

      if (strategyTypeCommands == null)
      {
        strategyTypeCommands = new ObservableCollection<CreateTypeCommand>();
        foreach (Type strategyType in this.StrategyTypes)
        {
          Peach.Core.MutationStrategyAttribute sa = (from object o in strategyType.GetCustomAttributes(typeof(Peach.Core.MutationStrategyAttribute), false) select o as Peach.Core.MutationStrategyAttribute).FirstOrDefault();
          strategyTypeCommands.Add(new CreateTypeCommand(sa.Name, strategyType, this.CreateChild));
        }
      }

      this.Children = WrapItems();
    }

    [Browsable(false)]
    public override string Image
    {
      get
      {
        return "Images/node-test.png";
      }
    }

    protected ObservableCollection<ViewModelBase> WrapItems()
    {
      ObservableCollection<ViewModelBase> vms = new ObservableCollection<ViewModelBase>();

      Test model = (Test)this.Model;
      var agentrefs = from Node n in model.Items where n is AgentReferenceNode select n as AgentReferenceNode;
      foreach (var agentref in agentrefs)
      {
        vms.Add(new ReferenceViewModel(agentref, this));
      }

      var includesexcludes = from Node n in model.Items where ((n is TestInclude) || (n is TestExclude)) select n;
      if (includesexcludes.Count() > 0)
      {
        CreateTestIncludesExcludesAssociation();
        foreach (Node n in includesexcludes)
        {
          tiea.Add(new TestIncludeExcludeViewModel(n, this));
        }
        vms.Add(tiea);
      }

      var stateModelRefs = from Node n in model.Items where n is StateModelReferenceNode select n as StateModelReferenceNode;
      foreach (StateModelReferenceNode stateModelRef in stateModelRefs)
      {
        vms.Add(new ReferenceViewModel(stateModelRef, this));
      }

      var logger = (from Node n in model.Items where n is Logger select n as Logger).FirstOrDefault();
      if (logger != null)
      {
        vms.Add(new LoggerViewModel(logger, this));
      }

      var publishers = from Node n in model.Items where n is Publisher select n as Publisher;
      if (publishers.Count() > 0)
      {
        CreateTestPublishersAssociation();
        foreach (Publisher p in publishers)
        {
          tpa.Add(new PublisherViewModel(p, this));
        }
        vms.Add(tpa);
      }

      var strategy = (from Node n in model.Items where n is Strategy select n as Strategy).FirstOrDefault();
      if (strategy != null)
      {
        vms.Add(new StrategyViewModel(strategy, this));
      }
      return vms;
    }

    internal override void ExecuteCreateChild(string parameter)
    {

      Type loggerType = (from t in this.LoggerTypes where t.FullName == parameter select t as Type).FirstOrDefault();
      if (loggerType != null)
      {
        if (this.Children.ContainsType(typeof(LoggerViewModel)))
        {
          this.PeachViewModel.RaiseMessage("Test \"{0}\" already contains a logger.", ((Test)this.Model).Name);
        }
        else
        {
          Logger logger = new Logger(loggerType);
          ((Test)Model).Items.Add(logger);
          LoggerViewModel lvm = new LoggerViewModel(logger, this);
          this.Children.Add(lvm);
        }
        return;
      }

      Type publisherType = (from t in this.PublisherTypes where t.FullName == parameter select t as Type).FirstOrDefault();
      if (publisherType != null)
      {
        CreateTestPublishersAssociation(true);
        Publisher publisher = new Publisher(publisherType);
        ((Test)Model).Items.Add(publisher);

        PublisherViewModel pvm = new PublisherViewModel(publisher, this);
        tpa.Add(pvm);
        return;
      }

      Type strategyType = (from t in this.StrategyTypes where t.FullName == parameter select t as Type).FirstOrDefault();
      if (strategyType != null)
      {
        var existingStrategy = (from vm in this.Children where vm is StrategyViewModel select vm).FirstOrDefault();
        if (existingStrategy == null)
        {
          Strategy strategy = new Strategy(strategyType);
          ((Test)Model).Items.Add(strategy);

          StrategyViewModel svm = new StrategyViewModel(strategy, this);
          this.Children.Add(svm);
        }
        else
        {
          throw new Peach.Core.PeachException("Tests can contain only one Strategy.");
        }
        return;
      }

      switch (parameter)
      {
        case "Agent":
          AgentReferenceNode agent = new AgentReferenceNode();
          ((Test)Model).Items.Add(agent);

          this.Children.Add(new ReferenceViewModel(agent, this));
          break;
        case "TestExclude":
          CreateTestIncludesExcludesAssociation(true);
          TestExclude exclude = new TestExclude();
          ((Test)Model).Items.Add(exclude);

          TestIncludeExcludeViewModel evm = new TestIncludeExcludeViewModel(exclude, this);
          tiea.Add(evm);
          break;
        case "TestInclude":
          CreateTestIncludesExcludesAssociation(true);
          TestInclude include = new TestInclude();
          ((Test)Model).Items.Add(include);

          TestIncludeExcludeViewModel ivm = new TestIncludeExcludeViewModel(include, this);
          tiea.Add(ivm);
          break;
        case "StateModelReference":
          StateModelReferenceNode smr = new StateModelReferenceNode();
          ((Test)Model).Items.Add(smr);

          ReferenceViewModel rvm = new ReferenceViewModel(smr, this);
          this.Children.Add(rvm);
          break;
      }
    }

    protected override void DeleteChild(ViewModelBase viewModel)
    {
      ((Models.Test)Model).Items.Remove(viewModel.Model);
      switch (viewModel.GetType().Name)
      {
        case "TestIncludeExcludeViewModel":
          this.tiea.Remove(viewModel);
          break;
        case "PublisherViewModel":
          this.tpa.Remove(viewModel);
          break;
        default:
          this.Children.Remove(viewModel);
          break;
      }
    }

    public override void ReorderItems()
    {
      ObservableCollection<object> newItems = new ObservableCollection<object>();
      foreach (ViewModelBase vm in tiea.Children)
      {
        newItems.Add(vm.Model);
      }

      foreach (ViewModelBase vm in this.Children)
      {
        if (vm != tiea)
        {
          newItems.Add(vm.Model);
        }
      }
      ((Test)Model).Items = newItems;
    }

    #region LoggersTypeCommands Property

    private ObservableCollection<CreateTypeCommand> loggerTypeCommands;

    [Browsable(false)]
    public ObservableCollection<CreateTypeCommand> LoggerTypeCommands
    {
      get
      {
        return this.loggerTypeCommands;
      }
      set
      {
        if (this.loggerTypeCommands != value)
        {
          this.loggerTypeCommands = value;
          RaisePropertyChanged("LoggerTypeCommands");
        }
      }
    }

    #endregion

    #region PublisherTypeCommands Property

    private ObservableCollection<CreateTypeCommand> publisherTypeCommands;

    [Browsable(false)]
    public ObservableCollection<CreateTypeCommand> PublisherTypeCommands
    {
      get
      {
        return this.publisherTypeCommands;
      }
      set
      {
        if (this.publisherTypeCommands != value)
        {
          this.publisherTypeCommands = value;
          RaisePropertyChanged("PublisherTypeCommands");
        }
      }
    }

    #endregion

    #region StrategyTypeCommands Property

    private ObservableCollection<CreateTypeCommand> strategyTypeCommands;

    [Browsable(false)]
    public ObservableCollection<CreateTypeCommand> StrategyTypeCommands
    {
      get
      {
        return this.strategyTypeCommands;
      }
      set
      {
        if (this.strategyTypeCommands != value)
        {
          this.strategyTypeCommands = value;
          RaisePropertyChanged("StrategyTypeCommands");
        }
      }
    }

    #endregion

    private void CreateTestIncludesExcludesAssociation(bool addToTree = false)
    {
      if (tiea == null)
      {
        tiea = new TestIncludesExcludesAssociation(this);
        if (addToTree)
          this.Children.Add(tiea);
      }
    }

    private void CreateTestPublishersAssociation(bool addToTree = false)
    {
      if (tpa == null)
      {
        tpa = new TestPublishersAssociation(this);
        if (addToTree)
          this.Children.Add(tpa);
      }
    }

    //private void CreateTestLoggersAssociation(bool addToTree = false)
    //{
    //  if (tla == null)
    //  {
    //    tla = new TestLoggersAssociation(this);
    //    if (addToTree)
    //      this.Children.Add(tla);
    //  }
    //}

  }
}
