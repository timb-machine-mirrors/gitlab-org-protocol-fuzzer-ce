using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PitMaker.Models
{
  [System.SerializableAttribute()]
  [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = false, Namespace = "http://phed.org/2012/Peach")]
  public class Ruby : Node
  {
    #region Code Property

    private string codeField = String.Empty;

    [System.Xml.Serialization.XmlAttribute(AttributeName = "code")]
    public string Code
    {
      get
      {
        return this.codeField;
      }
      set
      {
        if (this.codeField != value)
        {
          this.codeField = value;
          RaisePropertyChanged("Code");
        }
      }
    }

    #endregion
  }
}
