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
  public class Strategy : NodeWithParameters
  {
    public Strategy() { }

    public Strategy(Type strategyType) 
      :base(strategyType)
    {
      if (strategyType.IsSubclassOf(typeof(Peach.Core.MutationStrategy)) == false)
      {
        throw new Peach.Core.PeachException(strategyType.Name + " is not a valid Mutation Strategy type.");
      }
      Peach.Core.MutationStrategyAttribute sa = (from object o in strategyType.GetCustomAttributes(typeof(Peach.Core.MutationStrategyAttribute), false) select o as Peach.Core.MutationStrategyAttribute).FirstOrDefault();
      this.StrategyClass = sa.Name;
    }

    #region Class Property

    private string classField;

    [Browsable(false)]
    [XmlAttribute(AttributeName="class")]
    public string StrategyClass
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
          RaisePropertyChanged("StrategyClass");
        }
      }
    }

    #endregion

  }
}
