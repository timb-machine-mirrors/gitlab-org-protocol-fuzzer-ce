using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace PitMaker.Models
{
  [System.SerializableAttribute()]
  [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = false, Namespace = "http://peachfuzzer.com/2012/Peach")]
  public class Import : Node
  {
    public Import() { }

    #region Import Property

    private string importField = String.Empty;

    [XmlAttribute(AttributeName="import")]
    public string Module
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
          RaisePropertyChanged("Module");
        }
      }
    }

    #endregion

    
  }
}
