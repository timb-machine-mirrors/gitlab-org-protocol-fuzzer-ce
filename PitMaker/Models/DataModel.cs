using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Xml.Serialization;
using System.ComponentModel;
using System.Xml.Linq;
using System.Xml;
using System.IO;
using System.Reflection;

namespace PitMaker.Models
{
  [System.SerializableAttribute()]
  //[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = false, Namespace = "http://phed.org/2012/Peach", TypeName="DataModel")]
	[System.Xml.Serialization.XmlRoot(DataType="DataModel",ElementName="DataModel",IsNullable=false,Namespace = "http://phed.org/2012/Peach")]
	//[System.Xml.Serialization.XmlSchemaProvider("MySchema")]
  public class DataModel : Node, IXmlSerializable
  {
    public DataModel() { }


    #region Items Property
    private ObservableCollection<object> itemsField = new ObservableCollection<object>();


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
        if (this.itemsField != value)
        {
          this.itemsField = value;
          RaisePropertyChanged("Items");
        }
      }
    }
    #endregion

    #region Name Property

    private string nameField = "dataModel";

    [Category(Categories.Required)]
    [System.Xml.Serialization.XmlAttributeAttribute(AttributeName = "name")]
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

    #region Constraint Property

    private string constraintField = String.Empty;

    [Category(Categories.Optional)]
    [XmlAttribute(AttributeName = "constraint")]
    public string Constraint
    {
      get
      {
        return this.constraintField;
      }
      set
      {
        if (this.constraintField != value)
        {
          this.constraintField = value;
          RaisePropertyChanged("Constraint");
        }
      }
    }

    #endregion

    #region Mutable Property

    private bool mutableField = false;

    [Category(Categories.Required)]
    [XmlAttribute(AttributeName = "mutable")]
    public bool Mutable
    {
      get
      {
        return this.mutableField;
      }
      set
      {
        if (this.mutableField != value)
        {
          this.mutableField = value;
          RaisePropertyChanged("Mutable");
        }
      }
    }

    #endregion


    public System.Xml.Schema.XmlSchema GetSchema()
    {
      return null;
    }

    public void ReadXml(System.Xml.XmlReader reader)
    {
      XElement node = (XElement)XDocument.ReadFrom(reader);

      List<XAttribute> xmlattributes = node.Attributes().ToList();

      var result = (from XAttribute a in xmlattributes where a.Name.LocalName == "name" select a).FirstOrDefault();
      if (result != null)
        this.Name = result.Value;

      result = (from XAttribute a in xmlattributes where a.Name.LocalName == "mutable" select a).FirstOrDefault();
      if (result != null)
        this.Mutable = bool.Parse(result.Value);
      
      result = (from XAttribute a in xmlattributes where a.Name.LocalName == "constraint" select a).FirstOrDefault();
      if (result != null)
        this.Constraint = result.Value;

      foreach (XElement child in node.Elements())
      {
        string elementName = child.Name.LocalName;
        XmlSerializer serializer = null;
        switch (elementName)
        {
          case "Fixup":
            serializer = new XmlSerializer(typeof(Fixup));
            this.Items.Add((Fixup)serializer.Deserialize(child.CreateReader()));
            break;
          case "Hint":
            serializer = new XmlSerializer(typeof(Fixup));
            this.Items.Add((Hint)serializer.Deserialize(child.CreateReader()));
            break;
          case "Placement":
            serializer = new XmlSerializer(typeof(Fixup));
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
                throw new Peach.Core.PeachException(child.Name.LocalName + " is not a valid child of " + node.Name.LocalName);
            } 
            break;
          case "Transformer":
            serializer = new XmlSerializer(typeof(Fixup));
            this.Items.Add((Transformer)serializer.Deserialize(child.CreateReader()));
            break;
          default: //DataElement
            List<Type> dataElementTypes = (from Type t in Peach.Core.ClassLoader.GetAllTypesByAttribute<Peach.Core.Dom.DataElementAttribute>(null) where t.IsSubclassOf(typeof(Peach.Core.Dom.DataElement)) || t.IsSubclassOf(typeof(Peach.Core.Dom.DataElementContainer)) select t).ToList();
            
            
            XmlRootAttribute xRoot = new XmlRootAttribute();
            xRoot.ElementName = elementName;
            xRoot.IsNullable = true;

            Type dataElementType = (from Type t in dataElementTypes where t.Name == elementName select t).FirstOrDefault();

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
              throw new Peach.Core.PeachException("Element not recognized: " + this.Name);
            }
            break;
        }
      }
    }

    public void WriteXml(System.Xml.XmlWriter writer)
    {
      writer.WriteAttributeString("name", this.nameField);

      if(this.mutableField)
        writer.WriteAttributeString("mutable", this.mutableField.ToString().ToLower());

      if(String.IsNullOrEmpty(this.constraintField) == false)
        writer.WriteAttributeString("constraint", this.constraintField.ToString());

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

		public static XmlQualifiedName MySchema(System.Xml.Schema.XmlSchemaSet xs)
		{
			// This method is called by the framework to get the schema for this type. 
			// We return an existing schema from disk.

			XmlSerializer schemaSerializer = new XmlSerializer(typeof(System.Xml.Schema.XmlSchema));
			using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("PitMaker.DataModel.xsd"))
			{
				System.Xml.Schema.XmlSchema s = (System.Xml.Schema.XmlSchema)schemaSerializer.Deserialize(stream);
				xs.XmlResolver = new XmlUrlResolver();
				xs.Add(s);
			}

			return new XmlQualifiedName("dataModel", "http://phed.org/2012/Peach");
		}
  }
}
