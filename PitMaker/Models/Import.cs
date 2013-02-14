using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace PitMaker.Models
{
  [System.SerializableAttribute()]
  [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://phed.org/2012/Peach")]
  [System.Xml.Serialization.XmlRootAttribute(ElementName="Import", Namespace = "http://phed.org/2012/Peach", IsNullable = false)]
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
