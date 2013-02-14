using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Reflection;

using Telerik.Windows.Controls.Data.PropertyGrid;
using System.Windows.Input;
using System.Windows.Data;
using System.Diagnostics;
using System.Dynamic;
using Microsoft.Practices.Prism.Commands;
using PitMaker.Models;
using Microsoft.Win32;



namespace PitMaker.ViewModels
{
  
  class ViewModelBase : DynamicObject, INotifyPropertyChanged
  {
    public ViewModelBase(Node model, ViewModelBase parent)
    {
      this.CreateChild = new DelegateCommand<string>(ExecuteCreateChild, (p) => { return true; });
      this.Delete = new DelegateCommand(ExecuteDelete, () => { return true; });
      this.SelectFile = new DelegateCommand<string>(ExecuteSelectFile);

      if (model != null)
      {
        this.Model = model;
        this.Model.Message += new EventHandler<Node.MessageEventArgs>(Model_Message);
      }

      if (parent != null)
      {
        this.Parent = parent;
        this.PeachViewModel = parent.PeachViewModel;
      }
      else
      {
        Debug.WriteLine(this.GetType().Name);
      }

      this.PropertyChanged += new PropertyChangedEventHandler(OnPropertyChanged);

      if (DataElementTypes == null)
        DataElementTypes = (from Type t in Peach.Core.ClassLoader.GetAllTypesByAttribute<Peach.Core.Dom.DataElementAttribute>(null) where t.IsSubclassOf(typeof(Peach.Core.Dom.DataElement)) || t.IsSubclassOf(typeof(Peach.Core.Dom.DataElementContainer)) select t).ToList();

      #region FixupTypes
      if (this.FixupTypes == null)
      {
        this.FixupTypes = Peach.Core.ClassLoader.GetAllTypesByAttribute<Peach.Core.FixupAttribute>(null).Distinct().ToList();
      }
      #endregion

      #region AnalyzerTypes
      if (this.AnalyzerTypes == null)
      {
        this.AnalyzerTypes = Peach.Core.ClassLoader.GetAllTypesByAttribute<Peach.Core.AnalyzerAttribute>(null).Distinct().ToList();
      }
      #endregion

      #region TransformerTypes
      if (this.TransformerTypes == null)
      {
        this.TransformerTypes = Peach.Core.ClassLoader.GetAllTypesByAttribute<Peach.Core.TransformerAttribute>(null).Distinct().ToList();
      }
      #endregion

      #region PublisherTypes
      if (this.PublisherTypes == null)
      {
        this.PublisherAltNames = Peach.Core.ClassLoader.GetAllByAttribute<Peach.Core.PublisherAttribute>(null).ToDictionary(kv => kv.Key.Name, kv => kv.Value);
        this.PublisherTypes = this.PublisherAltNames.Values.Distinct().ToList();
      }
      #endregion

      #region LoggerTypes
      if (this.LoggerTypes == null)
      {
        this.LoggerAltNames = Peach.Core.ClassLoader.GetAllByAttribute<Peach.Core.LoggerAttribute>(null).ToDictionary(kv => kv.Key.Name, kv => kv.Value);
        this.LoggerTypes = this.LoggerAltNames.Values.Distinct().ToList();
      }
      #endregion

      #region MonitorTypes
      if (this.MonitorTypes == null)
      {
        this.MonitorAltNames = Peach.Core.ClassLoader.GetAllByAttribute<Peach.Core.Agent.MonitorAttribute>(null).GroupBy(kv => kv.Key.Name).Select(grp => grp.First()).ToDictionary(kv => kv.Key.Name, kv => kv.Value);
        this.MonitorTypes = this.MonitorAltNames.Values.Distinct().ToList();
      }
      #endregion
    }

    void Model_Message(object sender, Node.MessageEventArgs e)
    {
      this.PeachViewModel.RaiseMessage(e.Message);
    }

    void Model_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
      if (e is PropertyChangedEventArgsEx)
      {
        PropertyChangedEventArgsEx ex = (PropertyChangedEventArgsEx)e;
        RaisePropertyChanged(ex.PropertyName, ex.OldValue, ex.NewValue);
      }
      else
      {
        RaisePropertyChanged(e.PropertyName);
      }
    }

    protected virtual void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {

    }

    public virtual void ReorderItems()
    {
      throw new NotImplementedException();
    }

    protected virtual PropertyDefinitionCollection CreatePropertyDefinitions()
    {
      return new PropertyDefinitionCollection();
    }

    #region Dynamic Object overrides
    //*
    public override bool TryGetMember(GetMemberBinder binder, out object result)
    {
      string propertyName = binder.Name;
      PropertyInfo property = this.GetType().GetProperty(propertyName);

      if (property == null || property.CanRead == false)
      {
        if (this.Model != null)
        {
          return this.Model.TryGetMember(binder, out result);
        }
        else
        {
          result = null;
          return false;
        }
      }
      else
      {
        result = property.GetValue(this, null);
        return true;
      }
    }

    public override bool TrySetMember(SetMemberBinder binder, object value)
    {
      string propertyName = binder.Name;
      PropertyInfo property = this.GetType().GetProperty(propertyName);

      if (property == null || property.CanWrite == false)
      {
        if (this.Model != null)
        {
          return this.Model.TrySetMember(binder, value);
        }
        else
        {
          return false;
        }
      }
      else
      {
        property.SetValue(this, value, null);
        return true;
      }
    }

    private List<string> memberNames = null;
    public override IEnumerable<string> GetDynamicMemberNames()
    {
      if (memberNames == null)
      {
        memberNames = new List<string>(this.GetType().GetBrowsablePropertyNames());
        if (this.Model != null)
        {
          var modelMemberNames = this.Model.GetDynamicMemberNames();
          foreach (string modelMemberName in modelMemberNames)
          {
            memberNames.Add(modelMemberName);
          }
        }
      }

      return (IEnumerable<string>)memberNames;
    }

    
    //*/
    #endregion

    #region Properties

    #region Image Property

    private string imageField = "Images/node-unknown.png";

    [Browsable(false)]
    public virtual string Image
    {
      get
      {
        return this.imageField;
      }
      set
      {
        if (this.imageField != value)
        {
          this.imageField = value;
          RaisePropertyChanged("Image");
        }
      }
    }

    #endregion

    #region IsExpanded Property

    private bool isExpandedField = false;

    [Browsable(false)]
    public bool IsExpanded
    {
      get
      {
        return this.isExpandedField;
      }
      set
      {
        if (this.isExpandedField != value)
        {
          this.isExpandedField = value;
          RaisePropertyChanged("IsExpanded");
        }
      }
    }

    #endregion

    #region Model Property

    private Node modelField;

    [Browsable(false)]
    public Node Model
    {
      get { return modelField; }
      set
      { 
        modelField = value;
        if (modelField != null)
        {
          this.Model.PropertyChanged -= Model_PropertyChanged;
          this.Model.PropertyChanged += Model_PropertyChanged;
        }
      }
    }

    #endregion

    #region Parent Property

    private ViewModelBase parentField;

    [Browsable(false)]
    public ViewModelBase Parent
    {
      get
      {
        return this.parentField;
      }
      set
      {
        if (this.parentField != value)
        {
          this.parentField = value;
          RaisePropertyChanged("Parent");
        }
      }
    }

    #endregion

    #region Children Property

    private ObservableCollection<ViewModelBase> childrenField = new ObservableCollection<ViewModelBase>();

    [Browsable(false)]
    public ObservableCollection<ViewModelBase> Children
    {
      get
      {
        return childrenField;
      }
      set
      {
        if (childrenField != value)
        {
          childrenField = value;
          RaisePropertyChanged("Children");
        }
      }
    }

    #endregion

    #region IsDirty Property

    private static bool isDirtyField;

    [Browsable(false)]
    public bool IsDirty
    {
      get
      {
        return isDirtyField;
      }
      set
      {
        if (isDirtyField != value)
        {
          isDirtyField = value;
          if (PropertyChanged != null)
          {
            PropertyChanged(this, new PropertyChangedEventArgs("IsDirty"));
          }
          Debug.WriteLine("Dirty=" + isDirtyField);
        }
      }
    }

    #endregion

    #region StrategyTypes Property

    [Browsable(false)]
    public List<Type> StrategyTypes
    {
      get
      {
        return Peach.Core.ClassLoader.GetAllTypesByAttribute<Peach.Core.MutationStrategyAttribute>(null).ToList();
      }
    }

    #endregion

    #region DataElementTypes Property

    private static List<Type> dataElementTypesField;

    [Browsable(false)]
    public List<Type> DataElementTypes
    {
      get
      {
        return dataElementTypesField;
      }
      set
      {
        if (dataElementTypesField != value)
        {
          dataElementTypesField = value;
          RaisePropertyChanged("DataElementTypes");
        }
      }
    }

    #endregion

    #region PropertyDefinitions Property

    private PropertyDefinitionCollection propertyDefinitionsField;
    
    [Browsable(false)]
    public PropertyDefinitionCollection PropertyDefinitions
    {
      get
      {
        return this.propertyDefinitionsField;
      }
      set
      {
        if (this.propertyDefinitionsField != value)
        {
          this.propertyDefinitionsField = value;
          RaisePropertyChanged("PropertyDefinitions");
          RaisePropertyChanged("HasPropertyDefinitions");
        }
      }
    }

    #endregion

    #region HasPropertyDefinitions Property

    [Browsable(false)]
    public bool HasPropertyDefinitions
    {
      get
      {
        return !((PropertyDefinitions != null) && (PropertyDefinitions.Count > 0));
      }
    }

    #endregion

    #region FixupTypes Property
    private static List<Type> fixupTypesField;
    [Browsable(false)]
    public List<Type> FixupTypes
    {
      get { return fixupTypesField; }
      set { fixupTypesField = value; }
    }

    #endregion

    #region AnalyzerTypes Property
    private static List<Type> analyzerTypesField;
    [Browsable(false)]
    public List<Type> AnalyzerTypes
    {
      get { return analyzerTypesField; }
      set { analyzerTypesField = value; }
    }

    #endregion

    #region TransformerTypes Property
    private static List<Type> transformerTypesField;
    [Browsable(false)]
    public List<Type> TransformerTypes
    {
      get { return transformerTypesField; }
      set { transformerTypesField = value; }
    }

    #endregion

    #region PeachViewModel Property

    private PeachViewModel peachViewModelField = null;

    [Browsable(false)]
    public PeachViewModel PeachViewModel
    {
      get
      {
        if (this.peachViewModelField == null)
        {
          if (this.Parent != null)
          {
            if (this.Parent is PeachViewModel)
              peachViewModelField = (PeachViewModel)this.Parent;
            else
              peachViewModelField = this.Parent.PeachViewModel;
          }
        }
        return peachViewModelField;
      }
      set
      {
        if (this.peachViewModelField != value)
        {
          this.peachViewModelField = value;
        }
      }
    }

    #endregion

    #region PeachPitDocument Property

    [Browsable(false)]
    public PeachPitDocument PeachPitDocument
    {
      get
      {
        return (PeachPitDocument)this.PeachViewModel.Model;
      }
    }

    #endregion

    #region LoggerTypes Property
    private static List<Type> loggerTypesField;
    [Browsable(false)]
    public List<Type> LoggerTypes
    {
      get { return loggerTypesField; }
      set { loggerTypesField = value; }
    }

    #endregion

    #region LoggerAltNames Property

    private static Dictionary<string, Type> loggerAltNamesField = null;

    [Browsable(false)]
    public Dictionary<string, Type> LoggerAltNames
    {
      get { return loggerAltNamesField; }
      set { loggerAltNamesField = value; }
    }

    #endregion

    #region PublisherTypes Property
    private static List<Type> publisherTypesField;
    [Browsable(false)]
    public List<Type> PublisherTypes
    {
      get { return publisherTypesField; }
      set { publisherTypesField = value; }
    }
    #endregion

    #region PublisherAltNames Property

    private static Dictionary<string, Type> publisherAltNamesField = null;

    [Browsable(false)]
    public Dictionary<string, Type> PublisherAltNames
    {
      get { return publisherAltNamesField; }
      set { publisherAltNamesField = value; }
    }

    #endregion

    #region MonitorTypes Property
    private static List<Type> monitorTypesField;
    [Browsable(false)]
    public List<Type> MonitorTypes
    {
      get { return monitorTypesField; }
      set { monitorTypesField = value; }
    }

    #endregion

    #region MonitorAltNames Property

    private static Dictionary<string, Type> monitorAltNamesField = null;

    [Browsable(false)]
    public Dictionary<string, Type> MonitorAltNames
    {
      get { return monitorAltNamesField; }
      set { monitorAltNamesField = value; }
    }

    #endregion


    #endregion

    #region Commands

    #region CreateChild Command

    [Browsable(false)]
    public ICommand CreateChild { get; set; }

    internal virtual void ExecuteCreateChild(string parameter)
    {
      throw new NotImplementedException();
    }
    #endregion

    #region Delete Command

    [Browsable(false)]
    public ICommand Delete { get; set; }

    protected void ExecuteDelete()
    {
      this.Parent.DeleteChild(this);
    }
    #endregion

    protected virtual void DeleteChild(ViewModelBase viewModel)
    {
      throw new NotImplementedException();
    }

    #region SelectFile

    //  Put the following line in the constructor for this class
    //  this.SelectFileCommand = new DelegateCommand(SelectFile, CanSelectFile);

    [Browsable(false)]
    public ICommand SelectFile
    {
      get;
      set;
    }

    public void ExecuteSelectFile(string propertyName)
    {
      OpenFileDialog ofd = new OpenFileDialog();
      ofd.Filter = "All Files (*.*)|*.*";
      ofd.Multiselect = false;
      if (ofd.ShowDialog() == true)
      {
        if (Model.GetType().IsSubclassOf(typeof(NodeWithParameters)))
        {
          ((NodeWithParameters)Model).SetMember(propertyName, ofd.FileName);
        }
        else
        {
          Model.SetMember(propertyName, ofd.FileName);
        }
      }
    }

    #endregion

    #endregion

    #region INotifyPropertyChanged
    public event PropertyChangedEventHandler PropertyChanged;

    internal void RaisePropertyChanged(string propertyName)
    {
      if (PropertyChanged != null)
      {
        PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        if(propertyName != "IsDirty")
          IsDirty = true;
      }
    }

    internal void RaisePropertyChanged(string propertyName, object oldValue, object newValue)
    {
      if (PropertyChanged != null)
      {
        PropertyChanged(this, new PropertyChangedEventArgsEx(propertyName, oldValue, newValue));
        if (propertyName != "IsDirty")
          IsDirty = true;
      }
    }
    #endregion
  }
}
