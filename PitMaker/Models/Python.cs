using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PitMaker.Models
{
  [System.SerializableAttribute()]
  [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://phed.org/2012/Peach")]
  [System.Xml.Serialization.XmlRoot(Namespace = "http://phed.org/2012/Peach", IsNullable = false)]
  public class Python : Node
  {
    public Python() { }

    #region Import Property

    private string importField = String.Empty;

    [System.Xml.Serialization.XmlAttribute(AttributeName = "code")]
    public string Code
    {
      get
      {
        return this.importField;
      }
      set
      {
        if (this.importField != value)
        {
          this.importField = value;
          RaisePropertyChanged("Code");
        }
      }
    }

    #endregion

  }
}
