using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Xml.Serialization;
using System.ComponentModel;
using System.Xml.Linq;
using System.Dynamic;
using System.Xml;

namespace PitMaker.Models
{
  [System.SerializableAttribute()]
  [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = false, Namespace = "http://peachfuzzer.com/2012/Peach")]
  public class DataElementContainer : NodeWithParameters,IXmlSerializable
  {
    public DataElementContainer() { }

    public DataElementContainer(Type dataElementContainerType) 
      :base(dataElementContainerType)
    {
      if (dataElementContainerType.IsSubclassOf(typeof(Peach.Core.Dom.DataElementContainer)) == false)
      {
        throw new Peach.Core.PeachException(dataElementContainerType.Name + " is not a valid Data Element Container type.");
      }

      this.PeachType = dataElementContainerType;
    }

    [Browsable(false)]
    public string Name
    {
      get
      {
        return (string)Parameters["name"];
      }
      set
      {
        if (((string)Parameters["name"]) != value)
        {
          Parameters["name"] = value;
          RaisePropertyChanged("Name");
        }
      }
    }

    #region Items Property
    private ObservableCollection<object> itemsField = new ObservableCollection<object>();

    //[System.Xml.Serialization.XmlElementAttribute("Custom", typeof(Custom))]
    //[System.Xml.Serialization.XmlElementAttribute("Seek", typeof(Seek))]

    [Browsable(false)]
    [System.Xml.Serialization.XmlElementAttribute(typeof(DataElement))]
    [System.Xml.Serialization.XmlElementAttribute(typeof(DataElementContainer))]
    [System.Xml.Serialization.XmlElementAttribute("Fixup", typeof(Fixup))]
    [System.Xml.Serialization.XmlElementAttribute("Hint", typeof(Hint))]
    [System.Xml.Serialization.XmlElementAttribute("Placement", typeof(Placement))]
    [System.Xml.Serialization.XmlElementAttribute("Relation", typeof(Relation))]
    [System.Xml.Serialization.XmlElementAttribute("Transformer", typeof(Transformer))]
    public ObservableCollection<object> Items
    {
      get
      {
        return this.itemsField;
      }
      set
      {
        if ((this.itemsField != null))
        {
          if ((itemsField.Equals(value) != true))
          {
            this.itemsField = value;
            this.RaisePropertyChanged("Items");
          }
        }
        else
        {
          this.itemsField = value;
          this.RaisePropertyChanged("Items");
        }
      }
    }
    #endregion

    #region IXmlSerializable

    public System.Xml.Schema.XmlSchema GetSchema()
    {
      return null;
    }

    public void ReadXml(System.Xml.XmlReader reader)
    {
      reader.MoveToContent();
      XElement element = (XElement)XDocument.ReadFrom(reader);

      List<Type> dataElementTypes = (from Type t in Peach.Core.ClassLoader.GetAllTypesByAttribute<Peach.Core.Dom.DataElementAttribute>(null) where t.IsSubclassOf(typeof(Peach.Core.Dom.DataElement)) || t.IsSubclassOf(typeof(Peach.Core.Dom.DataElementContainer)) select t).ToList();
      this.PeachType = (from Type t in dataElementTypes where t.Name == element.Name.LocalName select t).FirstOrDefault();

      if (this.PeachType != null)
      {
        List<XAttribute> xmlattributes = element.Attributes().ToList();

        foreach (string memberName in MemberNames)
        {
          var attrib = element.Attribute(memberName);
          if (attrib != null)
          {
            Parameters[memberName] = attrib.Value;
          }
        }
      }
      else
      {
        throw new Peach.Core.PeachException("Element type not recognized: " + element.Name.LocalName);
      }

      foreach (XElement child in element.Elements())
      {
        XmlSerializer serializer = null;
        switch (child.Name.LocalName)
        {
          case "Param":
            // do nothing
            break;
          case "Analyzer":
            serializer = new XmlSerializer(typeof(Analyzer));
            this.Items.Add((Analyzer)serializer.Deserialize(child.CreateReader()));
            break;
          case "Fixup":
            serializer = new XmlSerializer(typeof(Fixup));
            this.Items.Add((Fixup)serializer.Deserialize(child.CreateReader()));
            break;
          case "Hint":
            serializer = new XmlSerializer(typeof(Hint));
            this.Items.Add((Hint)serializer.Deserialize(child.CreateReader()));
            break;
          case "Placement":
            serializer = new XmlSerializer(typeof(Placement));
            this.Items.Add((Placement)serializer.Deserialize(child.CreateReader()));
            break;
          case "Relation":
            switch (child.Attribute("type").Value)
            {
              case "size":
                serializer = new XmlSerializer(typeof(SizeRelation));
                this.Items.Add((SizeRelation)serializer.Deserialize(child.CreateReader()));
                break;
              case "count":
                serializer = new XmlSerializer(typeof(CountRelation));
                this.Items.Add((CountRelation)serializer.Deserialize(child.CreateReader()));
                break;
              case "offset":
                serializer = new XmlSerializer(typeof(OffsetRelation));
                this.Items.Add((OffsetRelation)serializer.Deserialize(child.CreateReader()));
                break;
              default:
                throw new Peach.Core.PeachException(child.Attribute("type").Value + " is not a valid Relation type.");
            }
            break;
          case "Transformer":
            serializer = new XmlSerializer(typeof(Transformer));
            this.Items.Add((Transformer)serializer.Deserialize(child.CreateReader()));
            break;
          default:
            Type dataElementType = (from Type t in dataElementTypes where t.Name == child.Name.LocalName select t).FirstOrDefault();
            if (dataElementType != null)
            {
              if (dataElementType.IsSubclassOf(typeof(Peach.Core.Dom.DataElementContainer)))
              {
                DataElementContainer dec = new DataElementContainer(dataElementType);
                dec.ReadXml(child.CreateReader());
                this.Items.Add(dec);
              }
              else if (dataElementType.IsSubclassOf(typeof(Peach.Core.Dom.DataElement)))
              {
                DataElement de = new DataElement(dataElementType);
                de.ReadXml(child.CreateReader());
                this.Items.Add(de);
              }
            }
            else
            {
              throw new Peach.Core.PeachException("Element name not recognized: " + child.Name.LocalName);
            }
            break;
        }
      }
    }

    public void WriteXml(System.Xml.XmlWriter writer)
    {

      foreach (string a in MemberNames)
      {
        try
        {
          Peach.Core.ParameterAttribute pa = Parameters.GetKey(a);
          object value = Parameters[pa];
          Type valueType = value.GetType();

          if (pa.required)
          {
            if (valueType == typeof(bool))
            {
              writer.WriteAttributeString(pa.name, value.ToString().ToLower());
            }
            else
            {
              writer.WriteAttributeString(pa.name, value.ToString());
            }
          }
          else
          {
            if (String.Compare(value.ToString().ToLower(), pa.defaultValue.ToLower()) != 0)
            {
              if (valueType == typeof(string))
              {
                if (String.IsNullOrEmpty((string)value) == false)
                  writer.WriteAttributeString(pa.name, (string)value);
              }
              else if (valueType == typeof(bool))
              {
                if ((bool)value)
                  writer.WriteAttributeString(pa.name, "true");
              }
              else if (value != null)
              {
                writer.WriteAttributeString(pa.name, value.ToString());
              }
            }
          }
        }
        catch { }
      }

      foreach (object item in this.Items)
      {
        Type type = item.GetType();

        if (type == typeof(DataElement))
        {
          writer.WriteStartElement(((DataElement)item).PeachType.Name);
          ((DataElement)item).WriteXml(writer);
          writer.WriteEndElement();
        }
        else if (type == typeof(DataElementContainer))
        {
          writer.WriteStartElement(((DataElementContainer)item).PeachType.Name);
          ((DataElementContainer)item).WriteXml(writer);
          writer.WriteEndElement();
        }
        else
        {
          XmlSerializer serializer = new XmlSerializer(type);
          serializer.Serialize(writer, item);
        }
      }
    }

    #endregion

  }


}
