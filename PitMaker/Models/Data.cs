using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PitMaker.Models
{
  [System.SerializableAttribute()]
  [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = false, Namespace = "http://phed.org/2012/Peach")]
  public class Data : Node
  {
    public Data() { }

    #region Name Property

    private string nameField = "data";

    [System.Xml.Serialization.XmlAttribute("name")]
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

    #region FileName Property

    private string fileNameField = String.Empty;

    [System.Xml.Serialization.XmlAttribute("fileName")]
    public string FileName
    {
      get
      {
        return this.fileNameField;
      }
      set
      {
        if (this.fileNameField != value)
        {
          this.fileNameField = value;
          RaisePropertyChanged("FileName");
        }
      }
    }

    #endregion


  }
}
