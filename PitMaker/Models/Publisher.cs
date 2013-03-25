using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Xml.Serialization;
using System.ComponentModel;

namespace PitMaker.Models
{
  [System.SerializableAttribute()]
  [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = false, Namespace = "http://phed.org/2012/Peach")]
  public class Publisher : NodeWithParameters
  {
    public Publisher() { }

    public Publisher(Type publisherType)
      :base(publisherType)
    {
      if (publisherType.IsSubclassOf(typeof(Peach.Core.Publisher)) == false)
      {
        throw new Peach.Core.PeachException(publisherType.Name + " is not a valid Publisher type.");
      }
      Peach.Core.PublisherAttribute pa = (from object o in publisherType.GetCustomAttributes(typeof(Peach.Core.PublisherAttribute), false) where ((Peach.Core.PublisherAttribute)o).IsDefault select o as Peach.Core.PublisherAttribute).FirstOrDefault();
      this.PublisherClass = pa.Name;
      this.Name = pa.Name;
    }

    #region Name Property

    private string nameField = String.Empty;

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

    #region Class Property

    private string classField = String.Empty;

    [XmlAttribute(AttributeName="class")]
    [Browsable(false)]
    public string PublisherClass
    {
      get
      {
        return this.classField;
      }
      set
      {
        if (this.classField != value)
        {
          this.classField = value;
          RaisePropertyChanged("Class");
        }
      }
    }

    #endregion

  }
}
