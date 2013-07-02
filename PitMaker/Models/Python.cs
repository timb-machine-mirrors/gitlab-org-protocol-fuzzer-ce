using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PitMaker.Models
{
  [System.SerializableAttribute()]
  [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = false, Namespace = "http://peachfuzzer.com/2012/Peach")]
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
