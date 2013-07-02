using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Xml.Serialization;
using System.Dynamic;
using System.ComponentModel;

namespace PitMaker.Models
{
  [System.SerializableAttribute()]
  [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = false, Namespace = "http://peachfuzzer.com/2012/Peach")]
  public class Logger : NodeWithParameters
  {

    public Logger() { }

    public Logger(Type loggerType)
      :base(loggerType)
    {
      if (loggerType.IsSubclassOf(typeof(Peach.Core.Logger)) == false)
      {
        throw new Peach.Core.PeachException(loggerType.Name + " is not a valid Logger type.");
      }
      Peach.Core.LoggerAttribute la = (from object o in loggerType.GetCustomAttributes(typeof(Peach.Core.LoggerAttribute), false) where o is Peach.Core.LoggerAttribute && ((Peach.Core.LoggerAttribute)o).IsDefault select o as Peach.Core.LoggerAttribute).FirstOrDefault();
      this.LoggerClass = la.Name;
    }

    #region Class Property

    private string classField;

    [Browsable(false)]
    [XmlAttribute(AttributeName="class")]
    public string LoggerClass
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
          RaisePropertyChanged("LoggerClass");
        }
      }
    }

    #endregion



  }
}
