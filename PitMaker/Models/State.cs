using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Collections.ObjectModel;

namespace PitMaker.Models
{
  [System.SerializableAttribute()]
  [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = false, Namespace = "http://peachfuzzer.com/2012/Peach")]
  public class State : Node
  {
    #region Name Property

    private string nameField = "state";

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
          object old = this.nameField;
          this.nameField = value;
          RaisePropertyChanged("Name", old, value);
        }
      }
    }

    #endregion


    #region Items Property

    private ObservableCollection<object> itemsField = new ObservableCollection<object>();

    [System.ComponentModel.Browsable(false)]
    [XmlElement("Action",typeof(Action))]
    public ObservableCollection<object> Items
    {
      get
      {
        return this.itemsField;
      }
      set
      {
        if (this.itemsField != value)
        {
          this.itemsField = value;
          RaisePropertyChanged("Items");
        }
      }
    }

    #endregion


  }
}
