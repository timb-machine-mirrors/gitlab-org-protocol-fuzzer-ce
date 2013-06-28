using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Dynamic;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace PitMaker.Models
{
  [Serializable()]
  [XmlType(AnonymousType = false, Namespace = "http://peachfuzzer.com/2012/Peach")]
  [XmlRoot(Namespace = "http://peachfuzzer.com/2012/Peach", IsNullable = false)]
  public class Agent : Node
  {
    public Agent()
    {
      
    }
    
    #region Name Property

    private string nameField = "agent";

		[Category(Categories.Required)]
    [XmlAttribute(AttributeName = "name")]
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

    #region Password Property

    private string passwordField = String.Empty;

    [Category(Categories.Optional)]
    [XmlAttribute(AttributeName = "password")]
    public string Password
    {
      get
      {
        return this.passwordField;
      }
      set
      {
        if (this.passwordField != value)
        {
          this.passwordField = value;
          RaisePropertyChanged("Password");
        }
      }
    }

    #endregion

    #region Location Property

    private string locationField = "local://";

		[Category(Categories.Required)]
		[XmlAttribute(AttributeName = "location")]
    public string Location
    {
      get
      {
        return this.locationField;
      }
      set
      {
        if (this.locationField != value)
        {
          this.locationField = value;
          RaisePropertyChanged("Location");
        }
      }
    }

    #endregion

    #region Items Property

    private ObservableCollection<object> itemsField = new ObservableCollection<object>();

    [Browsable(false)]
    [XmlElement("Monitor", typeof(Monitor))]
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
