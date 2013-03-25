using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace PitMaker.Models
{
  [System.SerializableAttribute()]
  [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = false, Namespace = "http://phed.org/2012/Peach")]
  public class Include : Node
  {
    public Include() { }

    #region Namespace Property

    private string namespaceField = String.Empty;

    [XmlAttribute(AttributeName="ns")]
    public string Namespace
    {
      get
      {
        return this.namespaceField;
      }
      set
      {
        if (this.namespaceField != value)
        {
          this.namespaceField = value;
          RaisePropertyChanged("Namespace");
        }
      }
    }

    #endregion

    #region Source Property

    private string sourceField = String.Empty;

    [XmlAttribute(AttributeName="src")]
    public string Source
    {
      get
      {
        return this.sourceField;
      }
      set
      {
        if (this.sourceField != value)
        {
          this.sourceField = value;
          RaisePropertyChanged("Source");
        }
      }
    }

    #endregion
  }
}
