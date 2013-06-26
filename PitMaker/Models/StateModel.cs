using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace PitMaker.Models
{
  [System.SerializableAttribute()]
  [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = false, Namespace = "http://peachfuzzer.com/2012/Peach")]
  public class StateModel : Node
  {
    #region Name Property

    private string nameField = "stateModel";

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
          this.nameField = value;
          RaisePropertyChanged("Name");
        }
      }
    }

    #endregion

    #region InitialState Property

    private string initialStateField = String.Empty;

    [XmlAttribute(AttributeName="initialState")]
    public string InitialState
    {
      get
      {
        return this.initialStateField;
      }
      set
      {
        if (this.initialStateField != value)
        {
          this.initialStateField = value;
          RaisePropertyChanged("InitialState");
        }
      }
    }

    #endregion

    #region Items Property

    private ObservableCollection<State> itemsField = new ObservableCollection<State>();

    [XmlElement("State", Type=typeof(State), Order=0)]
    [Browsable(false)]
    public ObservableCollection<State> Items
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
