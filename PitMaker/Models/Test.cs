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
  [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://phed.org/2012/Peach")]
  [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://phed.org/2012/Peach", IsNullable = false)]
  public class Test : Node
  {
    public Test() { }

    #region Name Property

    private string nameField = "test";

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

    #region WaitTime Property

    private string waitTimeField = "0";

    [XmlAttribute(AttributeName="waitTime")]
    public string WaitTime
    {
      get
      {
        return this.waitTimeField;
      }
      set
      {
        if (this.waitTimeField != value)
        {
          this.waitTimeField = value;
          RaisePropertyChanged("WaitTime");
        }
      }
    }

    #endregion
    
    #region Items Property

    private ObservableCollection<object> itemsField = new ObservableCollection<object>();

    [Browsable(false)]
    [System.Xml.Serialization.XmlElementAttribute("Agent", typeof(AgentReferenceNode))]
    [System.Xml.Serialization.XmlElementAttribute("Exclude", typeof(TestExclude))]
    [System.Xml.Serialization.XmlElementAttribute("Include", typeof(TestInclude))]
    [System.Xml.Serialization.XmlElementAttribute("Logger", typeof(Logger))]
    [System.Xml.Serialization.XmlElementAttribute("Publisher", typeof(Publisher))]
    [System.Xml.Serialization.XmlElementAttribute("StateModel", typeof(StateModelReferenceNode))]
    [System.Xml.Serialization.XmlElementAttribute("Strategy", typeof(Strategy))]
    //[System.Xml.Serialization.XmlElementAttribute("Mutator", typeof(Mutator))]
    public ObservableCollection<object> Items
    {
      get
      {
        return this.itemsField;
      }
      set
      {
        if (this.itemsField != value)
        {
          this.itemsField = value;
          RaisePropertyChanged("Items");
        }
      }
    }

    #endregion
  }

  [System.SerializableAttribute()]
  [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://phed.org/2012/Peach")]
  [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://phed.org/2012/Peach", IsNullable = false, ElementName="Include")]
  public class TestInclude : Node
  {
    #region XPath Property

    private string xpathField = String.Empty;

    [XmlAttribute("xpath")]
    public string XPath
    {
      get
      {
        return this.xpathField;
      }
      set
      {
        if (this.xpathField != value)
        {
          this.xpathField = value;
          RaisePropertyChanged("XPath");
        }
      }
    }

    #endregion
  }

  [System.SerializableAttribute()]
  [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://phed.org/2012/Peach")]
  [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://phed.org/2012/Peach", IsNullable = false, ElementName="Exclude")]
  public class TestExclude : Node
  {
    #region XPath Property

    private string xpathField = String.Empty;

    [XmlAttribute("xpath")]
    public string XPath
    {
      get
      {
        return this.xpathField;
      }
      set
      {
        if (this.xpathField != value)
        {
          this.xpathField = value;
          RaisePropertyChanged("XPath");
        }
      }
    }

    #endregion
  }
}
