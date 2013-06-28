﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace PitMaker.Models
{
  [System.SerializableAttribute()]
  [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = false, Namespace = "http://peachfuzzer.com/2012/Peach")]
  public class Placement : Node
  {
    public Placement() { }

    #region After Property

    private string afterField = String.Empty;

    [XmlAttribute(AttributeName="after")]
    public string After
    {
      get
      {
        return this.afterField;
      }
      set
      {
        if (this.afterField != value)
        {
          this.afterField = value;
          RaisePropertyChanged("After");
        }
      }
    }

    #endregion

    #region Before Property

    private string beforeField = String.Empty;

    [XmlAttribute(AttributeName="before")]
    public string Before
    {
      get
      {
        return this.beforeField;
      }
      set
      {
        if (this.beforeField != value)
        {
          this.beforeField = value;
          RaisePropertyChanged("Before");
        }
      }
    }

    #endregion
  }
}
