﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PitMaker.Models
{
  [System.SerializableAttribute()]
  [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://phed.org/2012/Peach")]
  [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://phed.org/2012/Peach", IsNullable = false)]
  public class RubyPath : Node
  {
    #region Require Property

    private string requireField = String.Empty;

    [System.Xml.Serialization.XmlAttribute(AttributeName="require")]
    public string Require
    {
      get
      {
        return this.requireField;
      }
      set
      {
        if (this.requireField != value)
        {
          this.requireField = value;
          RaisePropertyChanged("Require");
        }
      }
    }

    #endregion

  }
}
