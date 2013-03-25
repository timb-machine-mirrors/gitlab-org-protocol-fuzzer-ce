using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Dynamic;
using System.ComponentModel;
using Microsoft.Practices.Prism.Commands;
using System.Reflection;
using System.Collections.ObjectModel;
using System.Xml.Serialization;
using System.Diagnostics;

namespace PitMaker.Models
{
  public class Node : DynamicObject, INotifyPropertyChanged
  {
    public Node()
    {
    }

    public bool GetMember(string propertyName, out object result)
    {
      PropertyInfo property = this.GetType().GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

      if (property == null || property.CanRead == false)
      {
        result = null;
        return false;
      }
      else
      {
        result = property.GetValue(this, null);
        return true;
      }
    }

    public bool SetMember(string propertyName, object value)
    {
      PropertyInfo property = this.GetType().GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

      if (property == null || property.CanWrite == false)
      {
        return false;
      }
      else
      {
        bool set = false;
        try
        {
          property.SetValue(this, value, null);
          set = true;
        }
        catch { }

        if (set == false)
        {
          property.SetValue(this, value.ToString());
        }
        return true;
      }
    }

    #region DynamicObject overrides
    public override bool TryGetMember(GetMemberBinder binder, out object result)
    {
      string propertyName = binder.Name;
      return GetMember(propertyName, out result);
    }

    public override bool TrySetMember(SetMemberBinder binder, object value)
    {
      string propertyName = binder.Name;
      return SetMember(propertyName, value);
    }
    //*/
    List<string> propertyNames;
    public override IEnumerable<string> GetDynamicMemberNames()
    {
      if(propertyNames == null)
        propertyNames = this.GetType().GetBrowsablePropertyNames();

      return propertyNames;
    }
    #endregion

    #region PeachType Property

    private Type peachTypeField;

    [Browsable(false)]
    [XmlIgnore]
    public Type PeachType
    {
      get
      {
        return this.peachTypeField;
      }
      set
      {
        if (this.peachTypeField != value)
        {
          this.peachTypeField = value;
          RaisePropertyChanged("PeachType");
        }
      }
    }

    #endregion

    internal virtual void SortItems()
    {

    }

    #region Message
    public event EventHandler<MessageEventArgs> Message;

    protected void RaiseMessage(string message)
    {
      if (Message != null)
      {
        Message(this, new MessageEventArgs(message));
      }
    }

    protected void RaiseMessage(string format, string arg0 = null, string arg1 = null, string arg2 = null)
    {
      RaiseMessage(String.Format(format, arg0, arg1, arg2));
    }

    public class MessageEventArgs : EventArgs
    {
      public MessageEventArgs(string message)
        :base()
      {
        this.Message = message;
      }

      public string Message { get; set; }
    }
    #endregion

    #region INotifyPropertyChanged
    public event PropertyChangedEventHandler PropertyChanged;

    internal void RaisePropertyChanged(string propertyName)
    {
      PropertyChangedEventArgs e = new PropertyChangedEventArgs(propertyName);
      if (PropertyChanged != null)
      {
        PropertyChanged(this, e);
        //if (propertyName != "IsDirty")
        //  IsDirty = true;
      }
      OnRaisePropertyChanged(e);
    }

    internal void RaisePropertyChanged(string propertyName, object oldValue, object newValue)
    {
      PropertyChangedEventArgsEx e = new PropertyChangedEventArgsEx(propertyName, oldValue, newValue);
      if (PropertyChanged != null)
      {
        PropertyChanged(this, e);
        //if (propertyName != "IsDirty")
        //  IsDirty = true;
      }
      OnRaisePropertyChanged(e);
    }

    protected virtual void OnRaisePropertyChanged(PropertyChangedEventArgs e)
    {

    }
    #endregion
  }

  public class NodeWithParameters : Node
  {
    public NodeWithParameters() 
    { 
      
    }

    public NodeWithParameters(Type t)
      :base()
    {
      this.PeachType = t;
    }

    protected override void OnRaisePropertyChanged(PropertyChangedEventArgs e)
    {
      if (e.PropertyName == "PeachType")
      {
        if ((Parameters == null) || (Parameters.Count == 0))
        {
          LoadParameters(PeachType);
        }
      }
    }

    public new bool GetMember(string propertyName, out object result)
    {
      if (base.GetMember(propertyName, out result) == false)
      {
        try
        {
          result = parameters[propertyName];
        }
        catch
        {
          result = null;
          return false;
        }
      }
      return true;
    }

    public new bool SetMember(string propertyName, object value)
    {
      if (base.SetMember(propertyName, value) == false)
      {
        try
        {
          parameters[propertyName] = value;

          var paramElement = (from Param p in this.Param where p.Name.ToLower() == propertyName.ToLower() select p).First();
          paramElement.Value = value.ToString();
        }
        catch(InvalidOperationException)
        {
          RaiseMessage("Invalid format: {0} was expecting type {1}", propertyName, parameters.GetKey(propertyName).type.FullName);
        }
        RaisePropertyChanged(propertyName);
      }

      return true;
    }

    #region Dynamic Object overrides

    public override bool TryGetMember(GetMemberBinder binder, out object result)
    {
      string propertyName = binder.Name;
      return GetMember(propertyName, out result);
    }

    public override bool TrySetMember(SetMemberBinder binder, object value)
    {
      string propertyName = binder.Name;
      return SetMember(propertyName, value);
    }

    private List<string> propertyNames;
    public override IEnumerable<string> GetDynamicMemberNames()
    {
      if(propertyNames == null)
        propertyNames = memberNames.Concat(base.GetDynamicMemberNames()).ToList();

      return propertyNames;
    }

    #endregion

    private ParameterValues parameters = null;

    [Browsable(false)]
    [XmlIgnore]
    public ParameterValues Parameters
    {
      get { return parameters; }
    }

    private List<string> memberNames = new List<string>();

    [Browsable(false)]
    [XmlIgnore]
    protected List<string> MemberNames
    {
      get { return memberNames; }
    }

    private static List<Type> nonInstantiableTypes = new List<Type>();

    [DebuggerNonUserCode]
    protected void LoadParameters(Type type)
    {
      if (parameters == null)
        parameters = new ParameterValues(type);
      else
        parameters.Clear();

      
      var attribs = Peach.Core.ClassLoader.GetAttributes<Peach.Core.ParameterAttribute>(type, null);


      foreach (Peach.Core.ParameterAttribute pa in attribs)
      {
        memberNames.Add(pa.name);

        object value = null;

        Param parameteritem = (from Param p in Param where p.Name == pa.name select p).FirstOrDefault();
        if (parameteritem == null)
        {
          if (pa.required)
          {
            #region required
            if (nonInstantiableTypes.Contains(pa.type) == false)
            {
              try
              {
                value = Activator.CreateInstance(pa.type);
              }
              catch
              {
                nonInstantiableTypes.Add(pa.type);
              }
            }

            // for special case types
            if (value == null)
            {
              switch (pa.type.Name)
              {
                case "String":
                case "DataElement":
                case "IPAddress":
                  if (pa.name.ToLower() == "name")
                    value = type.Name;
                  else
                    value = String.Empty;
                  break;
                default:
                  throw new NotSupportedException(pa.type.Name);
              }
            }
            #endregion
          }
          else
          { //optional
            value = Peach.Core.ParameterParser.FromString(type, pa, pa.defaultValue);
            if (value == null)
            {
              value = String.Empty;
            }
          }
          
          Param.Add(new Param(pa, value.ToString()));
        }
        else
        {
          value = Peach.Core.ParameterParser.FromString(type, pa, parameteritem.Value);
        }

        parameters.Add(pa, value);
        RaisePropertyChanged(pa.name.ToLower());
      }
    }

    #region Param Property

    private ObservableCollection<Param> paramField = new ObservableCollection<Param>();

    [Browsable(false)]
    [System.Xml.Serialization.XmlElementAttribute("Param")]
    public ObservableCollection<Param> Param
    {
      get
      {
        return this.paramField;
      }
      set
      {
        if (this.paramField != value)
        {
          this.paramField = value;
          RaisePropertyChanged("Param");
        }
      }
    }

    #endregion

  }

  public class ParameterValues : Dictionary<Peach.Core.ParameterAttribute, object>
  {
    private Type peachType = null;

    public ParameterValues(Type peachType)
    {
      this.peachType = peachType;
    }

    public object this[string index]
    {
      get
      {
        return (from Peach.Core.ParameterAttribute p in this.Keys where p.name.ToLower() == index.ToLower() select this[p]).FirstOrDefault();
      }
      set
      {
        Peach.Core.ParameterAttribute parameter = (from Peach.Core.ParameterAttribute p in this.Keys where p.name.ToLower() == index.ToLower() select p).FirstOrDefault();
        if ((parameter != null) && (this[parameter] != value))
        {
          if (value is System.String)
          {
            this[parameter] = Peach.Core.ParameterParser.FromString(peachType, parameter, (string)value);
          }
          else
          {
            this[parameter] = value;
          }
        }
      }
    }

    public bool HasKey(string key)
    {
      var result = (from Peach.Core.ParameterAttribute p in this.Keys where p.name.ToLower() == key.ToLower() select p).FirstOrDefault();
      return result != null;
    }

    public Peach.Core.ParameterAttribute GetKey(string key)
    {
      var result = (from Peach.Core.ParameterAttribute p in this.Keys where p.name.ToLower() == key.ToLower() select p).FirstOrDefault();
      return result;
    }
  }
}
