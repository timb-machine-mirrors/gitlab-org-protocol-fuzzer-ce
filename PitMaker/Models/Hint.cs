using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace PitMaker.Models
{
  [System.SerializableAttribute()]
  [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://phed.org/2012/Peach")]
  [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://phed.org/2012/Peach", IsNullable = false)]
  public class Hint : Node
  {
    public Hint() { }

    #region Name Property

    private string nameField = "hint";

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

    #region Value Property

    private string valueField = String.Empty;

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
