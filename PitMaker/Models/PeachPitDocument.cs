using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Practices.Prism.Commands;
using System.ComponentModel;

namespace PitMaker.Models
{
  [System.SerializableAttribute()]
  [System.Xml.Serialization.XmlRoot(Namespace = "http://peachfuzzer.com/2012/Peach", IsNullable = false, ElementName="Peach", DataType="Peach")]
  public class PeachPitDocument : Node
  {
    public PeachPitDocument()
    {
    }


    internal static PeachPitDocument LoadFromFile(string fileName)
    {
      PeachPitDocument peach = new PeachPitDocument();

      return peach;
    }

    public bool HasPython
    {
      get { return (from object o in this.Items where (o is Python) || (o is PythonPath) select o).Count() > 0; }
    }

    public bool HasRuby
    {
      get { return (from object o in this.Items where (o is Ruby) || (o is RubyPath) select o).Count() > 0; }
    }

    #region Items Property

    private ObservableCollection<object> itemsField = new ObservableCollection<object>();

    [Browsable(false)]
    [System.Xml.Serialization.XmlElementAttribute("Include", typeof(Include))]
    [System.Xml.Serialization.XmlElementAttribute("Require", typeof(Require))]
    [System.Xml.Serialization.XmlElementAttribute("Import", typeof(Import))]
    [System.Xml.Serialization.XmlElementAttribute("PythonPath", typeof(PythonPath))]
    [System.Xml.Serialization.XmlElementAttribute("RubyPath", typeof(RubyPath))]
    [System.Xml.Serialization.XmlElementAttribute("Python", typeof(Python))]
    [System.Xml.Serialization.XmlElementAttribute("Ruby", typeof(Ruby))]
    [System.Xml.Serialization.XmlElementAttribute("Defaults", typeof(Defaults))]
    [System.Xml.Serialization.XmlElementAttribute("DataModel", typeof(DataModel))]
    [System.Xml.Serialization.XmlElementAttribute("StateModel", typeof(StateModel))]
    [System.Xml.Serialization.XmlElementAttribute("Agent", typeof(Agent))]
    [System.Xml.Serialization.XmlElementAttribute("Test", typeof(Test))]
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

    #region Version Property

    private int versionField = 0;

    [XmlAttribute(AttributeName="version")]
    public int Version
    {
      get
      {
        return this.versionField;
      }
      set
      {
        if (this.versionField != value)
        {
          this.versionField = value;
          RaisePropertyChanged("Version");
        }
      }
    }

    #endregion

    #region Author Property

    private string authorField = String.Empty;

    [XmlAttribute(AttributeName="author")]
    public string Author
    {
      get
      {
        return this.authorField;
      }
      set
      {
        if (this.authorField != value)
        {
          this.authorField = value;
          RaisePropertyChanged("Author");
        }
      }
    }

    #endregion

    #region Description Property

    private string descriptionField = String.Empty;

    [XmlAttribute(AttributeName="description")]
    public string Description
    {
      get
      {
        return this.descriptionField;
      }
      set
      {
        if (this.descriptionField != value)
        {
          this.descriptionField = value;
          RaisePropertyChanged("Description");
        }
      }
    }

    #endregion

    internal override void SortItems()
    {
      

      var sortedItems = from object o in this.Items where o is Include select o;
      sortedItems = sortedItems.Concat(from object o in this.Items where o is Require select o);
      sortedItems = sortedItems.Concat(from object o in this.Items where o is Import select o);
      sortedItems = sortedItems.Concat(from object o in this.Items where o is PythonPath select o);
      sortedItems = sortedItems.Concat(from object o in this.Items where o is RubyPath select o);
      sortedItems = sortedItems.Concat(from object o in this.Items where o is Python select o);
      sortedItems = sortedItems.Concat(from object o in this.Items where o is Ruby select o);
      sortedItems = sortedItems.Concat(from object o in this.Items where o is Defaults select o);
      sortedItems = sortedItems.Concat(from object o in this.Items where o is DataModel select o);
      sortedItems = sortedItems.Concat(from object o in this.Items where o is StateModel select o);
      sortedItems = sortedItems.Concat(from object o in this.Items where o is Agent select o);
      sortedItems = sortedItems.Concat(from object o in this.Items where o is Test select o);

      this.Items = new ObservableCollection<object>(sortedItems);

      foreach (Node child in this.Items)
      {
        child.SortItems();
      }
    }
  }
}
