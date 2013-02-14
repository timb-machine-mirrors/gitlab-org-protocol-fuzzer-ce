using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Xml.Serialization;
using System.ComponentModel;

namespace PitMaker.Models
{
  [System.SerializableAttribute()]
  [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://phed.org/2012/Peach")]
  [System.Xml.Serialization.XmlRoot(Namespace = "http://phed.org/2012/Peach", IsNullable = false)]
  public class Fixup : NodeWithParameters
  {
    public Fixup() { }

    public Fixup(Type fixupType)
      :base(fixupType)
    {
      if (fixupType.IsSubclassOf(typeof(Peach.Core.Fixup)) == false)
      {
        throw new Peach.Core.PeachException(fixupType.Name + " is not a valid Fixup type.");
      }
      this.FixupClass = fixupType.Name;
    }

    #region Class Property

    private string classField;

    [Browsable(false)]
    [XmlAttribute(AttributeName = "class")]
    public string FixupClass
    {
      get
      {
        return this.classField;
      }
      set
      {
        if (this.classField != value)
        {
          string oldValue = this.classField;
          this.classField = value;
          RaisePropertyChanged("FixupClass", oldValue, value);
        }
      }
    }

    #endregion
  }
}
