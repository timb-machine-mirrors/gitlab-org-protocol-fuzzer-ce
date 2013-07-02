using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.ComponentModel;

namespace PitMaker.Models
{
  [System.SerializableAttribute()]
  [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = false, Namespace = "http://peachfuzzer.com/2012/Peach", TypeName="Relation")]
  [XmlInclude(typeof(CountRelation))]
  [XmlInclude(typeof(SizeRelation))]
  [XmlInclude(typeof(OffsetRelation))]
  public class Relation : Node
  {
    public Relation() { }

    #region Type Property
    private Peach.Core.Dom.DataElementRelations typeField;

    [Browsable(false)]
    [XmlAttribute(AttributeName="type")]
    public Peach.Core.Dom.DataElementRelations Type
    {
      get
      {
        return this.typeField;
      }
      set
      {
        if (this.typeField != value)
        {
          this.typeField = value;
          RaisePropertyChanged("Type");
        }
      }
    }
    #endregion

    #region Of Property

    private string ofField = String.Empty;

    [XmlAttribute(AttributeName="of")]
    public string Of
    {
      get
      {
        return this.ofField;
      }
      set
      {
        if (this.ofField != value)
        {
          this.ofField = value;
          RaisePropertyChanged("Of");
        }
      }
    }

    #endregion

    #region From Property
    /*
    private string fromField = String.Empty;

    [XmlAttribute(AttributeName="from")]
    public string From
    {
      get
      {
        return this.fromField;
      }
      set
      {
        if (this.fromField != value)
        {
          this.fromField = value;
          RaisePropertyChanged("From");
        }
      }
    }
    //*/
    #endregion

    #region ExpressionGet Property

    private string expressionGetField = String.Empty;

    [XmlAttribute(AttributeName="expressionGet")]
    public string ExpressionGet
    {
      get
      {
        return this.expressionGetField;
      }
      set
      {
        if (this.expressionGetField != value)
        {
          this.expressionGetField = value;
          RaisePropertyChanged("ExpressionGet");
        }
      }
    }

    #endregion

    #region ExpressionGet Property

    private string expressionSetField = String.Empty;

    [XmlAttribute(AttributeName = "expressionSet")]
    public string ExpressionSet
    {
      get
      {
        return this.expressionSetField;
      }
      set
      {
        if (this.expressionSetField != value)
        {
          this.expressionSetField = value;
          RaisePropertyChanged("ExpressionSet");
        }
      }
    }

    #endregion


  }

  //[System.SerializableAttribute()]
  //[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = false, Namespace = "http://peachfuzzer.com/2012/Peach", TypeName="Relation")]
  public class SizeRelation : Relation
  {
    public SizeRelation()
    {
      this.Type = Peach.Core.Dom.DataElementRelations.Size;
    }
  }

	//[System.SerializableAttribute()]
	//[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = false, Namespace = "http://peachfuzzer.com/2012/Peach")]
	//[System.Xml.Serialization.XmlRootAttribute(Namespace = "http://peachfuzzer.com/2012/Peach", IsNullable = false, ElementName = "Relation")]
  public class CountRelation : Relation
  {
    public CountRelation()
    {
      this.Type = Peach.Core.Dom.DataElementRelations.Count;
    }
  }

	//[System.SerializableAttribute()]
	//[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = false, Namespace = "http://peachfuzzer.com/2012/Peach")]
	//[System.Xml.Serialization.XmlRootAttribute(Namespace = "http://peachfuzzer.com/2012/Peach", IsNullable = false, ElementName = "Relation")]
  public class OffsetRelation : Relation
  {
    public OffsetRelation()
    {
      this.Type = Peach.Core.Dom.DataElementRelations.Offset;
    }

    #region RelativeTo Property

    private string relativeToField = String.Empty;

    [XmlAttribute(AttributeName = "relativeTo")]
    public string RelativeTo
    {
      get
      {
        return this.relativeToField;
      }
      set
      {
        if (this.relativeToField != value)
        {
          this.relativeToField = value;
          RaisePropertyChanged("Relative");
        }
      }
    }

    #endregion

    #region Relative Property

    private bool relativeField = false;

    [XmlAttribute("relative")]
    public bool Relative
    {
      get
      {
        return this.relativeField;
      }
      set
      {
        if (this.relativeField != value)
        {
          this.relativeField = value;
          RaisePropertyChanged("Relative");
        }
      }
    }

    #endregion
  }

  //[System.SerializableAttribute()]
  //[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = false, Namespace = "http://peachfuzzer.com/2012/Peach")]
  //public enum RelationType
  //{

  //  /// <remarks/>
  //  size,

  //  /// <remarks/>
  //  count,

  //  /// <remarks/>
  //  offset,
  //}
}
