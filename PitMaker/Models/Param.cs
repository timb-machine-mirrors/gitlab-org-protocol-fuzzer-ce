using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.ComponentModel;

namespace PitMaker.Models
{
  [System.SerializableAttribute()]
  [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://phed.org/2012/Peach")]
  [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://phed.org/2012/Peach", IsNullable = false)]
  public class Param : Node
  {
    public Param() { }

    public Param(string name)
    {
      this.Name = name;
      this.ValueType = typeof(string).ToString();
      this.Value = String.Empty;
    }

    public Param(string name, Type valueType)
    {
      this.Name = name;
      this.ValueType = "string";
      this.Value = GetDefaultValue(valueType);
    }

    public Param(string name, Type valueType, string value)
    {
      this.Name = name;
      this.ValueType = "string";
      this.Value = value;
    }

    public Param(Peach.Core.ParameterAttribute parameterAttribute)
    {
      this.Name = parameterAttribute.name;
      this.ValueType = "string";
      this.Value = GetDefaultValue(parameterAttribute.type);
    }

    public Param(Peach.Core.ParameterAttribute parameterAttribute, string value)
    {
      this.Name = parameterAttribute.name;
      this.ValueType = "string";
      this.Value = value;
    }

    private string GetDefaultValue(Type type)
    {
      object value = null;

      try
      {
        value = Activator.CreateInstance(type);
      }
      catch (MissingMemberException)
      {
        //Do nothing
      }

      // for special case types
      if (value == null)
      {
        switch (type.FullName)
        {
          case "System.String":
            value = String.Empty;
            break;
          case "Peach.Core.Dom.DataElement":
            value = String.Empty;
            break;
          default:
            throw new NotSupportedException(type.FullName);
        }
      }
      return value.ToString();
    }

    public override string ToString()
    {
      return this.Name;
    }

    #region Name Property

    private string nameField;

    [XmlAttribute(AttributeName="name")]
    [Browsable(false)]
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

    #region ValueType Property

    private string valueTypeField;

    [Browsable(false)]
    //[XmlAttribute(AttributeName="valueType")]
    [XmlIgnore]
    public string ValueType
    {
      get
      {
        return this.valueTypeField;
      }
      set
      {
        if (this.valueTypeField != value)
        {
          this.valueTypeField = value;
          RaisePropertyChanged("ValueType");
        }
      }
    }

    #endregion

    #region Value Property

    private string valueField;

    [XmlAttribute(AttributeName="value")]
    public string Value
    {
      get
      {
        return this.valueField;
      }
      set
      {
        if (this.valueField != value)
        {
          this.valueField = value;
          RaisePropertyChanged("Value");
        }
      }
    }

    #endregion
  }
}
