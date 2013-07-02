using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Xml.Serialization;
using System.ComponentModel;
using System.Xml.Linq;
using System.IO;
using System.Xml;


namespace PitMaker.Models
{
  [System.SerializableAttribute()]
  [System.Xml.Serialization.XmlType(Namespace = "http://peachfuzzer.com/2012/Peach", TypeName="Action")]
  public class Action : Node //, IXmlSerializable
  {
    public Action() { }

    public Action(ActionType actionType)
    {
      this.Type = actionType;
    }

    #region Name Property

    private string nameField = "action";

    [Category(Categories.Optional)]
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

    [Browsable(false)]
    [XmlIgnore]
    public bool NameSpecified { get; set; }

    #endregion

    #region When Property

    private string whenField = String.Empty;

    [Category(Categories.Optional)]
    [XmlAttribute(AttributeName = "when")]
    public string When
    {
      get
      {
        return this.whenField;
      }
      set
      {
        if (this.whenField != value)
        {
          this.whenField = value;
          RaisePropertyChanged("When");
        }
      }
    }

    [Browsable(false)]
    [XmlIgnore]
    public bool WhenSpecified { get; set; }
    #endregion

    #region Publisher Property

    private string publisherField = String.Empty;

    [Category(Categories.Optional)]
    [XmlAttribute(AttributeName = "publisher")]
    public string Publisher
    {
      get
      {
        return this.publisherField;
      }
      set
      {
        if (this.publisherField != value)
        {
          this.publisherField = value;
          RaisePropertyChanged("Publisher");
        }
      }
    }

    [Browsable(false)]
    [XmlIgnore]
    public bool PublisherSpecified { get; set; }
    #endregion

    #region OnStart Property

    private string onStartField = String.Empty;

    [Category(Categories.Optional)]
    [XmlAttribute(AttributeName = "onStart")]
    public string OnStart
    {
      get
      {
        return this.onStartField;
      }
      set
      {
        if (this.onStartField != value)
        {
          this.onStartField = value;
          RaisePropertyChanged("OnStart");
        }
      }
    }

    [Browsable(false)]
    [XmlIgnore]
    public bool OnStartSpecified { get; set; }
    #endregion

    #region OnComplete Property

    private string onCompleteField = String.Empty;

    [Category(Categories.Optional)]
    [XmlAttribute(AttributeName = "onComplete")]
    public string OnComplete
    {
      get
      {
        return this.onCompleteField;
      }
      set
      {
        if (this.onCompleteField != value)
        {
          this.onCompleteField = value;
          RaisePropertyChanged("OnComplete");
        }
      }
    }

    [Browsable(false)]
    [XmlIgnore]
    public bool OnCompleteSpecified { get; set; }

    #endregion

    #region ActionType Property

    private ActionType actionTypeField;

    [Browsable(false)]
    [XmlAttribute(AttributeName = "type")]
    public ActionType Type
    {
      get
      {
        return this.actionTypeField;
      }
      set
      {
        if (this.actionTypeField != value)
        {
          this.actionTypeField = value;
          RaisePropertyChanged("ActionType");
        }
      }
    }

    #endregion

    #region Items Property

    private ObservableCollection<object> itemsField = new ObservableCollection<object>();

    [Browsable(false)]
    [System.Xml.Serialization.XmlElementAttribute("Data", typeof(Data))]
    [System.Xml.Serialization.XmlElementAttribute("DataModel", typeof(DataModelReferenceNode))]
    [System.Xml.Serialization.XmlElementAttribute("Param", typeof(ActionParam))]
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

    #region Method Property

    private string methodField = String.Empty;

    [Browsable(false)]
    [XmlAttribute(AttributeName = "method")]
    public string Method
    {
      get
      {
        return this.methodField;
      }
      set
      {
        if (this.methodField != value)
        {
          this.methodField = value;
          RaisePropertyChanged("Method");
        }
      }
    }

    [Browsable(false)]
    [XmlIgnore]
    public bool MethodSpecified { get; set; }
    #endregion

    #region Property Property

    private string propertyField = String.Empty;

    [Browsable(false)]
    [XmlAttribute(AttributeName = "property")]
    public string Property
    {
      get
      {
        return this.propertyField;
      }
      set
      {
        if (this.propertyField != value)
        {
          this.propertyField = value;
          RaisePropertyChanged("Property");
        }
      }
    }

    [Browsable(false)]
    [XmlIgnore]
    public bool PropertySpecified { get; set; }
    #endregion

    #region SetXPath Property

    private string setXPathField = String.Empty;

    [Browsable(false)]
    [XmlAttribute(AttributeName = "setXpath")]
    public string SetXPath
    {
      get
      {
        return this.setXPathField;
      }
      set
      {
        if (this.setXPathField != value)
        {
          this.setXPathField = value;
          RaisePropertyChanged("SetXPath");
        }
      }
    }

    [Browsable(false)]
    [XmlIgnore]
    public bool SetXPathSpecified { get; set; }
    #endregion

    #region ValueXPath Property

    private string valueXPathField = String.Empty;

    [Browsable(false)]
    [XmlAttribute(AttributeName = "valueXpath")]
    public string ValueXPath
    {
      get
      {
        return this.valueXPathField;
      }
      set
      {
        if (this.valueXPathField != value)
        {
          this.valueXPathField = value;
          RaisePropertyChanged("ValueXPath");
        }
      }
    }

    [Browsable(false)]
    [XmlIgnore]
    public bool ValueXPathSpecified { get; set; }
    #endregion

    #region Ref Property

    private string referenceField = String.Empty;

    [Browsable(false)]
    [XmlAttribute(AttributeName = "ref")]
    public string Ref
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
          //RaisePropertyChanged("Reference");
        }
      }
    }

    [Browsable(false)]
    [XmlIgnore]
    public bool RefSpecified { get; set; }
    #endregion

    #region
    /*
    public System.Xml.Schema.XmlSchema GetSchema()
    {
      return null;
    }

    public void ReadXml(System.Xml.XmlReader reader)
    {
      XElement node = (XElement)XDocument.ReadFrom(reader);
      XAttribute a = null;
      
      if((a = node.Attribute("name")) != null)
      {
        this.Name = a.Value;
      }
      if((a = node.Attribute("when")) != null)
      {
        this.When = a.Value;
      }
      if ((a = node.Attribute("publisher")) != null)
      {
        this.Publisher = a.Value;
      }
      if ((a = node.Attribute("onStart")) != null)
      {
        this.OnStart = a.Value;
      }
      if((a = node.Attribute("onComplete")) != null)
      {
        this.OnComplete = a.Value;
      }
      if((a = node.Attribute("type")) != null)
      {
        this.Type = (ActionType)Enum.Parse(typeof(ActionType), a.Value, true);
      }
      if((a = node.Attribute("method")) != null)
      {
        this.Method = a.Value;
      }
      if((a = node.Attribute("property")) != null)
      {
        this.Property = a.Value;
      }
      if((a = node.Attribute("setXpath")) != null)
      {
        this.SetXPath = a.Value;
      }
      if((a = node.Attribute("valueXpath")) != null)
      {
        this.ValueXPath = a.Value;
      }
      if((a = node.Attribute("ref")) != null)
      {
        this.Ref = a.Value;
      }




      if (node.HasElements)
      {
        XmlSerializer serializer = null;
        foreach (XElement child in node.Elements())
        {
          switch (child.Name.LocalName)
          {
            case "Data":
              serializer = new XmlSerializer(typeof(Data));
              this.Items.Add((Data)serializer.Deserialize(child.CreateReader()));
              break;
            case "DataModel":
              serializer = new XmlSerializer(typeof(DataModelReferenceNode));
              this.Items.Add((DataModelReferenceNode)serializer.Deserialize(child.CreateReader()));
              break;
            case "Param":
              serializer = new XmlSerializer(typeof(ActionParam));
              this.Items.Add((ActionParam)serializer.Deserialize(child.CreateReader()));
              break;
          }
        }
      }
    }

    public void WriteXml(System.Xml.XmlWriter writer)
    {
      XmlSerializer serializer = null;
      writer.WriteAttributeString("type", this.Type.ToString());

      if (string.IsNullOrEmpty(this.nameField) == false)
        writer.WriteAttributeString("name", this.nameField);

      if (string.IsNullOrEmpty(this.whenField) == false)
        writer.WriteAttributeString("when", this.whenField);

      if (string.IsNullOrEmpty(this.publisherField) == false)
        writer.WriteAttributeString("publisher", this.publisherField);

      if (string.IsNullOrEmpty(this.onStartField) == false)
        writer.WriteAttributeString("onStart", this.onStartField);

      if (string.IsNullOrEmpty(this.onCompleteField) == false)
        writer.WriteAttributeString("onComplete", this.onCompleteField);
      
      if(string.IsNullOrEmpty(this.methodField) == false)
        writer.WriteAttributeString("method", this.methodField);

      if (string.IsNullOrEmpty(this.propertyField) == false)
        writer.WriteAttributeString("property", this.propertyField);

      if (string.IsNullOrEmpty(this.setXPathField) == false)
        writer.WriteAttributeString("setXpath", this.setXPathField);

      if (string.IsNullOrEmpty(this.valueXPathField) == false)
        writer.WriteAttributeString("valueXpath", this.valueXPathField);

      if (string.IsNullOrEmpty(this.referenceField) == false)
        writer.WriteAttributeString("ref", this.referenceField);

      if (this.Items.Count > 0)
      {
        foreach (object item in this.Items)
        {
          switch (item.GetType().Name)
          {
            case "Data":
              serializer = new XmlSerializer(typeof(Data));
              break;
            case "DataModelReferenceNode":
              serializer = new XmlSerializer(typeof(DataModelReferenceNode));
              break;
            case "ActionParam":
              serializer = new XmlSerializer(typeof(ActionParam));
              break;
          }
          serializer.Serialize(writer, item);
        }
      }
    }
    //*/
    #endregion
  }
  #region action classes
  /*
  [System.SerializableAttribute()]
  [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = false, Namespace = "http://peachfuzzer.com/2012/Peach")]
  [System.Xml.Serialization.XmlRoot(Namespace = "http://peachfuzzer.com/2012/Peach", IsNullable = false, ElementName="Action")]
  public class InputAction : Action
  {
    public InputAction()
    {
      this.Type = PitMaker.Models.ActionType.input;
    }

    #region Items Property

    private ObservableCollection<object> itemsField = new ObservableCollection<object>();

    [Browsable(false)]
    [System.Xml.Serialization.XmlElementAttribute("DataModel", typeof(DataModelReferenceNode))]
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
  [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = false, Namespace = "http://peachfuzzer.com/2012/Peach")]
  [System.Xml.Serialization.XmlRoot(Namespace = "http://peachfuzzer.com/2012/Peach", IsNullable = false, ElementName = "Action")]
  public class OutputAction : Action
  {
    public OutputAction()
    {
      this.Type = PitMaker.Models.ActionType.output;
    }

    #region Items Property

    private ObservableCollection<object> itemsField = new ObservableCollection<object>();

    [Browsable(false)]
    [System.Xml.Serialization.XmlElementAttribute("Data", typeof(Data))]
    [System.Xml.Serialization.XmlElementAttribute("DataModel", typeof(DataModelReferenceNode))]
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
  [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = false, Namespace = "http://peachfuzzer.com/2012/Peach")]
  [System.Xml.Serialization.XmlRoot(Namespace = "http://peachfuzzer.com/2012/Peach", IsNullable = false, ElementName = "Action")]
  public class ChangeStateAction : Action
  {
    public ChangeStateAction()
    {
      this.Type = PitMaker.Models.ActionType.changeState;
    }

    #region Reference Property

    private string referenceField = String.Empty;

    [XmlAttribute(AttributeName = "ref")]
    public string StateReference
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
          RaisePropertyChanged("StateReference");
        }
      }
    }

    #endregion
  }

  [System.SerializableAttribute()]
  [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = false, Namespace = "http://peachfuzzer.com/2012/Peach")]
  [System.Xml.Serialization.XmlRoot(Namespace = "http://peachfuzzer.com/2012/Peach", IsNullable = false, ElementName = "Action")]
  public class CallAction : Action
  {
    public CallAction()
    {
      this.Type = PitMaker.Models.ActionType.call;
    }

    #region Method Property

    private string methodField = String.Empty;

    [XmlAttribute(AttributeName = "method")]
    public string Method
    {
      get
      {
        return this.methodField;
      }
      set
      {
        if (this.methodField != value)
        {
          this.methodField = value;
          RaisePropertyChanged("Method");
        }
      }
    }

    #endregion

    #region Items Property

    private ObservableCollection<object> itemsField = new ObservableCollection<object>();

    //[System.Xml.Serialization.XmlElementAttribute("Result", typeof(Result))]
    [Browsable(false)]
    [System.Xml.Serialization.XmlElementAttribute("Param", typeof(ActionParam))]
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
  [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = false, Namespace = "http://peachfuzzer.com/2012/Peach")]
  [System.Xml.Serialization.XmlRoot(Namespace = "http://peachfuzzer.com/2012/Peach", IsNullable = false, ElementName = "Action")]
  public class GetPropAction : Action
  {
    public GetPropAction()
    {
      this.Type = PitMaker.Models.ActionType.getprop;
    }

    #region Property Property

    private string propertyField = String.Empty;

    [XmlAttribute(AttributeName = "property")]
    public string Property
    {
      get
      {
        return this.propertyField;
      }
      set
      {
        if (this.propertyField != value)
        {
          this.propertyField = value;
          RaisePropertyChanged("Property");
        }
      }
    }

    #endregion

    #region Items Property

    private ObservableCollection<object> itemsField = new ObservableCollection<object>();

    [Browsable(false)]
    [System.Xml.Serialization.XmlElementAttribute("DataModel", typeof(DataModelReferenceNode))]
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
  [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = false, Namespace = "http://peachfuzzer.com/2012/Peach")]
  [System.Xml.Serialization.XmlRoot(Namespace = "http://peachfuzzer.com/2012/Peach", IsNullable = false, ElementName = "Action")]
  public class SetPropAction : Action
  {
    public SetPropAction()
    {
      this.Type = PitMaker.Models.ActionType.setprop;
    }

    #region Property Property

    private string propertyField = String.Empty;

    [XmlAttribute(AttributeName = "property")]
    public string Property
    {
      get
      {
        return this.propertyField;
      }
      set
      {
        if (this.propertyField != value)
        {
          this.propertyField = value;
          RaisePropertyChanged("Property");
        }
      }
    }

    #endregion

    #region Items Property

    private ObservableCollection<object> itemsField = new ObservableCollection<object>();

    [Browsable(false)]
    [System.Xml.Serialization.XmlElementAttribute("Data", typeof(Data))]
    [System.Xml.Serialization.XmlElementAttribute("DataModel", typeof(DataModelReferenceNode))]
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
  [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = false, Namespace = "http://peachfuzzer.com/2012/Peach")]
  [System.Xml.Serialization.XmlRoot(Namespace = "http://peachfuzzer.com/2012/Peach", IsNullable = false, ElementName = "Action")]
  public class SlurpAction : Action
  {
    public SlurpAction()
    {
      this.Type = PitMaker.Models.ActionType.slurp;
    }

    #region SetXPath Property

    private string setXPathField = String.Empty;

    [XmlAttribute(AttributeName = "setXpath")]
    public string SetXPath
    {
      get
      {
        return this.setXPathField;
      }
      set
      {
        if (this.setXPathField != value)
        {
          this.setXPathField = value;
          RaisePropertyChanged("SetXPath");
        }
      }
    }

    #endregion

    #region ValueXPath Property

    private string valueXPathField = String.Empty;

    [XmlAttribute(AttributeName = "valueXpath")]
    public string ValueXPath
    {
      get
      {
        return this.valueXPathField;
      }
      set
      {
        if (this.valueXPathField != value)
        {
          this.valueXPathField = value;
          RaisePropertyChanged("ValueXPath");
        }
      }
    }

    #endregion

  }

  [System.SerializableAttribute()]
  [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = false, Namespace = "http://peachfuzzer.com/2012/Peach")]
  [System.Xml.Serialization.XmlRoot(Namespace = "http://peachfuzzer.com/2012/Peach", IsNullable = false, ElementName = "Action")]
  public class StartAction : Action
  {
    public StartAction()
    {
      this.Type = PitMaker.Models.ActionType.start;
    }
  }

  [System.SerializableAttribute()]
  [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = false, Namespace = "http://peachfuzzer.com/2012/Peach")]
  [System.Xml.Serialization.XmlRoot(Namespace = "http://peachfuzzer.com/2012/Peach", IsNullable = false, ElementName = "Action")]
  public class StopAction : Action
  {
    public StopAction()
    {
      this.Type = PitMaker.Models.ActionType.stop;
    }
  }

  [System.SerializableAttribute()]
  [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = false, Namespace = "http://peachfuzzer.com/2012/Peach")]
  [System.Xml.Serialization.XmlRoot(Namespace = "http://peachfuzzer.com/2012/Peach", IsNullable = false, ElementName = "Action")]
  public class ConnectAction : Action
  {
    public ConnectAction()
    {
      this.Type = PitMaker.Models.ActionType.connect;
    }
  }

  [System.SerializableAttribute()]
  [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = false, Namespace = "http://peachfuzzer.com/2012/Peach")]
  [System.Xml.Serialization.XmlRoot(Namespace = "http://peachfuzzer.com/2012/Peach", IsNullable = false, ElementName = "Action")]
  public class OpenAction : Action
  {
    public OpenAction()
    {
      this.Type = PitMaker.Models.ActionType.open;
    }
  }

  [System.SerializableAttribute()]
  [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = false, Namespace = "http://peachfuzzer.com/2012/Peach")]
  [System.Xml.Serialization.XmlRoot(Namespace = "http://peachfuzzer.com/2012/Peach", IsNullable = false, ElementName = "Action")]
  public class AcceptAction : Action
  {
    public AcceptAction()
    {
      this.Type = PitMaker.Models.ActionType.accept;
    }
  }

  [System.SerializableAttribute()]
  [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = false, Namespace = "http://peachfuzzer.com/2012/Peach")]
  [System.Xml.Serialization.XmlRoot(Namespace = "http://peachfuzzer.com/2012/Peach", IsNullable = false, ElementName = "Action")]
  public class CloseAction : Action
  {
    public CloseAction()
    {
      this.Type = PitMaker.Models.ActionType.close;
    }
  }
  //*/
  #endregion


  [System.SerializableAttribute()]
  [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = false, Namespace = "http://peachfuzzer.com/2012/Peach", TypeName="ActionParam")]
  public class ActionParam : Node
  {
    public ActionParam()
    {
    }

    #region Items Property

    private ObservableCollection<object> itemsField = new ObservableCollection<object>();

    [Browsable(false)]
    [System.Xml.Serialization.XmlElementAttribute("Data", typeof(Data))]
    [System.Xml.Serialization.XmlElementAttribute("DataModel", typeof(DataModelReferenceNode))]
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

  public enum ActionType
  {
    start,
    stop,

    accept,
    connect,
    open,
    close,

    input,
    output,

    call,
    setprop,
    getprop,

    changeState,
    slurp
  }
}
