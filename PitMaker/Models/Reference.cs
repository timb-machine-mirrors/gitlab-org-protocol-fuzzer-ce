using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace PitMaker.Models
{
  public class ReferenceNode : Node
  {
    public ReferenceNode() { }

  }

  public enum OS { None = 0, Windows = 1, OSX = 2, Linux = 4, Unix = 6, All = 7 };


  [Serializable()]
  [System.Xml.Serialization.XmlType(AnonymousType = true, Namespace = "http://phed.org/2012/Peach")]
  [System.Xml.Serialization.XmlRoot(Namespace = "http://phed.org/2012/Peach", IsNullable = false, ElementName = "Agent")]
  public class AgentReferenceNode : ReferenceNode
  {
    public AgentReferenceNode() { }

    #region AgentReference Property

    private string referenceField = String.Empty;

    [XmlAttribute("ref")]
    public string AgentReference
    {
      get
      {
        return this.referenceField;
      }
      set
      {
        if (this.referenceField != value)
        {
          this.referenceField = value;
          RaisePropertyChanged("AgentReference");
        }
      }
    }

    #endregion

    #region Platform Property


    private OS platformField = OS.Windows;

    [XmlAttribute(AttributeName = "platform")]
    public OS Platform
    {
      get
      {
        return this.platformField;
      }
      set
      {
        if (this.platformField != value)
        {
          this.platformField = value;
          RaisePropertyChanged("Platform");
        }
      }
    }

    #endregion
  }

  [System.SerializableAttribute()]
  [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://phed.org/2012/Peach")]
  [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://phed.org/2012/Peach", IsNullable = false, ElementName = "StateModel")]
  public class StateModelReferenceNode : ReferenceNode
  {
    public StateModelReferenceNode() { }

    #region Reference Property

    private string referenceField = String.Empty;

    [XmlAttribute("ref")]
    public string StateModelReference
    {
      get
      {
        return this.referenceField;
      }
      set
      {
        if (this.referenceField != value)
        {
          this.referenceField = value;
          RaisePropertyChanged("StateModelReference");
        }
      }
    }

    #endregion

  }

  [System.SerializableAttribute()]
  [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://phed.org/2012/Peach")]
  [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://phed.org/2012/Peach", IsNullable = false, ElementName = "Test")]
  public class TestReferenceNode : ReferenceNode
  {
    public TestReferenceNode() { }

    #region Reference Property

    private string referenceField = String.Empty;

    [XmlAttribute("ref")]
    public string TestReference
    {
      get
      {
        return this.referenceField;
      }
      set
      {
        if (this.referenceField != value)
        {
          this.referenceField = value;
          RaisePropertyChanged("TestReference");
        }
      }
    }

    #endregion
  }

  [Serializable()]
  [System.Xml.Serialization.XmlType(AnonymousType = true, Namespace = "http://phed.org/2012/Peach")]
  [System.Xml.Serialization.XmlRoot(Namespace = "http://phed.org/2012/Peach", IsNullable = false, ElementName = "DataModel")]
  public class DataModelReferenceNode : ReferenceNode
  {
    public DataModelReferenceNode() { }

    #region Reference Property

    private string referenceField = String.Empty;

    [XmlAttribute("ref")]
    public string DataModelReference
    {
      get
      {
        return this.referenceField;
      }
      set
      {
        if (this.referenceField != value)
        {
          this.referenceField = value;
          RaisePropertyChanged("DataModelReference");
        }
      }
    }

    #endregion

  }
}
