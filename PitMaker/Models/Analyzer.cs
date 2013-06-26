using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Xml.Serialization;
using Peach.Core;
using System.Reflection;
using System.Diagnostics;
using System.ComponentModel;

namespace PitMaker.Models
{
  [Serializable]
  [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = false, Namespace = "http://peachfuzzer.com/2012/Peach")]
  public class Analyzer : NodeWithParameters
  {
    public Analyzer() { }

    public Analyzer(Type analyzerType)
      :base(analyzerType)
    {
      if (analyzerType.IsSubclassOf(typeof(Peach.Core.Analyzer)) == false)
      {
        throw new Peach.Core.PeachException(analyzerType.Name + " is not a valid Analyzer type.");
      }

      this.AnalyzerClass = analyzerType.Name;
    }


    #region Class Property

    private string classField;

    [XmlAttribute(AttributeName="class")]
    [Browsable(false)]
    public string AnalyzerClass
    {
      get
      {
        return this.classField;
      }
      set
      {
        if (this.classField != value)
        {
          string oldValue = this.classField;
          this.classField = value;
          RaisePropertyChanged("AnalyzerClass", oldValue, value);
        }
      }
    }

    #endregion
  }
}
