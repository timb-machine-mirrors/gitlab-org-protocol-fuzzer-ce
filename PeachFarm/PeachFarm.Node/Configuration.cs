using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace PeachFarm.Node.Configuration
{
  public class NodeSection : ConfigurationSection
  {
    [ConfigurationProperty(Constants.Controller)]
    public Controller Controller
    {
      get { return (Controller)this[Constants.Controller]; }
      set { this[Constants.Controller] = value; }
    }

    [ConfigurationProperty(Constants.Output)]
    public Output Output
    {
      get { return (Output)this[Constants.Output]; }
      set { this[Constants.Output] = value; }
    }

    [ConfigurationProperty(Constants.Tags)]
    public TagsCollection Tags
    {
      get { return (TagsCollection)this[Constants.Tags]; }
      set { this[Constants.Tags] = value; }
    }
  }

  public class Output : ConfigurationElement
  {
    [ConfigurationProperty(Constants.OutputType)]
    public OutputType OutputType
    {
      get { return (OutputType)this[Constants.OutputType]; }
      set { this[Constants.OutputType] = value; }
    }
  }

  public class Controller : ConfigurationElement
  {
    [ConfigurationProperty(Constants.IPAddress)]
    public string IpAddress
    {
      get { return (string)this[Constants.IPAddress]; }
      set { this[Constants.IPAddress] = value; }
    }
  }

  public class TagsCollection : ConfigurationElementCollection
  {
    public override ConfigurationElementCollectionType CollectionType
    {
      get { return ConfigurationElementCollectionType.AddRemoveClearMap; }
    }

    protected override ConfigurationElement CreateNewElement()
    {
      return new Tag();
    }

    protected override object GetElementKey(ConfigurationElement element)
    {
      return ((Tag)element).Name;
    }

    public Tag this[int index]
    {
      get { return (Tag)BaseGet(index); }
      set
      {
        if (BaseGet(index) != null)
          BaseRemoveAt(index);
        BaseAdd(index, value);
      }
    }

    new public Tag this[string name]
    {
      get { return (Tag)BaseGet(name); }
    }
  }

  public class Tag : ConfigurationElement
  {
    [ConfigurationProperty(Constants.Name)]
    public string Name
    {
      get { return (string)this[Constants.Name]; }
      set { this[Constants.Name] = value; }
    }
  }

  public static class Constants
  {
    public const string Controller = "Controller";
    public const string IPAddress = "ipAddress";

    public const string Output = "Output";
    public const string OutputType = "type";

    public const string Tags = "Tags";
    public const string Tag = "Tag";
    public const string Name = "name";
  }

  public enum OutputType
  {
    Silent,
    Console,
    NLog
  }
}
