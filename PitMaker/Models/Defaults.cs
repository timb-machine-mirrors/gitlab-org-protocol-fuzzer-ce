using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Xml.Serialization;
using System.ComponentModel;

namespace PitMaker.Models
{
  public enum ByteOrder
  {
    little,
    big,
    network
  }

  [System.SerializableAttribute()]
  [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = false, Namespace = "http://peachfuzzer.com/2012/Peach")]
  public class Defaults : Node
  {
    #region Items Property

    private List<object> itemsField = new List<object>();

    [Browsable(false)]
    [XmlElement("Number", typeof(DefaultNumber))]
    [XmlElement("String", typeof(DefaultString))]
    [XmlElement("Flags", typeof(DefaultFlags))]
    [XmlElement("Blob", typeof(DefaultBlob))]
    public List<object> Items
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

  [System.SerializableAttribute()]
	[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = false, Namespace = "http://peachfuzzer.com/2012/Peach")]
  public class Default : Node 
  {
    #region DisplayName Property

    private string displayNameField;

    [Browsable(false)]
    [XmlIgnore]
    public string DisplayName
    {
      get
      {
        return this.displayNameField;
      }
      set
      {
        if (this.displayNameField != value)
        {
          this.displayNameField = value;
          RaisePropertyChanged("DisplayName");
        }
      }
    }

    #endregion
  }

  [System.SerializableAttribute()]
	[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = false, Namespace = "http://peachfuzzer.com/2012/Peach")]
  public class DefaultNumber : Default
  {
    public DefaultNumber()
    {
      this.DisplayName = "Numbers";
    }

    #region Size Property

    private int sizeField = 1;

    [XmlAttribute("size")]
    public int Size
    {
      get
      {
        return this.sizeField;
      }
      set
      {
        if (this.sizeField != value)
        {
          this.sizeField = value;
          RaisePropertyChanged("Size");
        }
      }
    }

    #endregion

    #region Endian Property

    private ByteOrder endianField;

    [XmlAttribute("endian")]
    public ByteOrder Endian
    {
      get
      {
        return this.endianField;
      }
      set
      {
        if (this.endianField != value)
        {
          this.endianField = value;
          RaisePropertyChanged("Endian");
        }
      }
    }

    #endregion

    #region Signed Property

    private bool signedField;

    [XmlAttribute("signed")]
    public bool Signed
    {
      get
      {
        return this.signedField;
      }
      set
      {
        if (this.signedField != value)
        {
          this.signedField = value;
          RaisePropertyChanged("Signed");
        }
      }
    }

    #endregion
  }

  [System.SerializableAttribute()]
	[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = false, Namespace = "http://peachfuzzer.com/2012/Peach")]
  public class DefaultString : Default
  {
    public DefaultString()
    {
      this.DisplayName = "Strings";
    }

    #region LengthType Property

    private string lengthTypeField = String.Empty;

    [XmlAttribute("lengthType")]
    public string LengthType
    {
      get
      {
        return this.lengthTypeField;
      }
      set
      {
        if (this.lengthTypeField != value)
        {
          this.lengthTypeField = value;
          RaisePropertyChanged("LengthType");
        }
      }
    }

    #endregion

    #region PadCharacter Property

    private string padCharacterField = String.Empty;

    [XmlAttribute("padCharacter")]
    public string PadCharacter
    {
      get
      {
        return this.padCharacterField;
      }
      set
      {
        if (this.padCharacterField != value)
        {
          this.padCharacterField = value;
          RaisePropertyChanged("PadCharacter");
        }
      }
    }

    #endregion

    #region Type Property

    private string typeField = String.Empty;

    [XmlAttribute("type")]
    public string Type
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

    #region NullTerminated Property

    private string nullTerminated = String.Empty;

    [XmlAttribute("nullTerminated")]
    public string NullTerminated
    {
      get
      {
        return this.nullTerminated;
      }
      set
      {
        if (this.nullTerminated != value)
        {
          this.nullTerminated = value;
          RaisePropertyChanged("NullTerminated");
        }
      }
    }

    #endregion


  }

  [System.SerializableAttribute()]
	[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = false, Namespace = "http://peachfuzzer.com/2012/Peach")]
  public class DefaultFlags : Default
  {
    public DefaultFlags()
    {
      this.DisplayName = "Flags";
    }

    #region Endian Property

    private ByteOrder endianField;

    [XmlAttribute("endian")]
    public ByteOrder Endian
    {
      get
      {
        return this.endianField;
      }
      set
      {
        if (this.endianField != value)
        {
          this.endianField = value;
          RaisePropertyChanged("Endian");
        }
      }
    }

    #endregion

    #region Size Property

    private int sizeField;

    [XmlAttribute("size")]
    public int Size
    {
      get
      {
        return this.sizeField;
      }
      set
      {
        if (this.sizeField != value)
        {
          this.sizeField = value;
          RaisePropertyChanged("Size");
        }
      }
    }

    #endregion


  }

  [System.SerializableAttribute()]
  [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = false, Namespace = "http://peachfuzzer.com/2012/Peach")]
  public class DefaultBlob : Default
  {
    public DefaultBlob()
    {
      this.DisplayName = "Blobs";
    }

    #region LengthType Property

    private Peach.Core.Dom.LengthType lengthType;

    [XmlAttribute("lengthType")]
    public Peach.Core.Dom.LengthType LengthType
    {
      get
      {
        return this.lengthType;
      }
      set
      {
        if (this.lengthType != value)
        {
          this.lengthType = value;
          RaisePropertyChanged("LengthType");
        }
      }
    }

    #endregion


  }
}
