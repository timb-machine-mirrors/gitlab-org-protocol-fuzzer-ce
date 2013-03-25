using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace PitMaker.Models
{
  [System.SerializableAttribute()]
  [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = false, Namespace = "http://phed.org/2012/Peach")]
  public class Monitor : NodeWithParameters
  {
    public Monitor()
    {

    }

    public Monitor(Type monitorType) 
      :base(monitorType)
    { 
      if(monitorType.IsSubclassOf(typeof(Peach.Core.Agent.Monitor)) == false)
      {
        throw new Peach.Core.PeachException(monitorType.Name + " is not a valid Monitor type.");
      }
      Peach.Core.Agent.MonitorAttribute ma = (from object o in monitorType.GetCustomAttributes(typeof(Peach.Core.Agent.MonitorAttribute), false) where ((Peach.Core.Agent.MonitorAttribute)o).IsDefault select o as Peach.Core.Agent.MonitorAttribute).FirstOrDefault();
      this.MonitorClass = ma.Name;
      if (String.IsNullOrEmpty(this.Name))
        this.Name = ma.Name;
    }

    #region Name Property

    private string nameField = String.Empty;

    [XmlAttribute(AttributeName="name")]
    public string Name
    {
      get
      {
        return this.nameField;
      }
      set
      {
        if (this.nameField != value)
        {
          this.nameField = value;
          RaisePropertyChanged("Name");
        }
      }
    }

    #endregion

    #region MonitorClass Property

    private string classField = String.Empty;

    [Browsable(false)]
    [XmlAttribute(AttributeName="class")]
    public string MonitorClass
    {
      get
      {
        return this.classField;
      }
      set
      {
        if (this.classField != value)
        {
          this.classField = value;
          RaisePropertyChanged("MonitorClass");
        }
      }
    }

    #endregion

  }
}
