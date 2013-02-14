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
  [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://phed.org/2012/Peach")]
  [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://phed.org/2012/Peach", IsNullable = false)]
  public class Transformer : Node
  {
    public Transformer() { }

    public Transformer(Type transformerType)
    {
      if (transformerType.IsSubclassOf(typeof(Peach.Core.Transformer)))
      {
        this.TransformerClass = transformerType.Name;
      }
      else
      {
        throw new Peach.Core.PeachException(transformerType.Name + " is not a valid Transformer type.");
      }
    }

    #region Class Property

    private string classField;

    [XmlAttribute(AttributeName="class")]
    [Browsable(false)]
    public string TransformerClass
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
          RaisePropertyChanged("TransformerClass");
        }
      }
    }

    #endregion

    #region Items Property

    private ObservableCollection<Transformer> paramField;

    [Browsable(false)]
    [System.Xml.Serialization.XmlElementAttribute("Transformer", typeof(Transformer))]
    public ObservableCollection<Transformer> Items
    {
      get
      {
        return this.paramField;
      }
      set
      {
        if (this.paramField != value)
        {
          this.paramField = value;
          RaisePropertyChanged("Param");
        }
      }
    }

    #endregion


  }
}
