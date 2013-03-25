using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PitMaker.Models
{
  [System.SerializableAttribute()]
  [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = false, Namespace = "http://phed.org/2012/Peach")]
  public class PythonPath : Node
  {
    public PythonPath() { }

    #region Import Property

    private string importField = String.Empty;

    [System.Xml.Serialization.XmlAttribute(AttributeName="import")]
    public string Import
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
          RaisePropertyChanged("Import");
        }
      }
    }

    #endregion
  }
}
