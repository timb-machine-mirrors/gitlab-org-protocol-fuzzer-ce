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
  }

  public class Output : ConfigurationElement
  {
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

  public static class Constants
  {
    public const string Controller = "Controller";
    public const string IPAddress = "ipAddress";

    public const string Output = "Output";
    public const string OutputType = "type";
  }

  public enum OutputType
  {
    Silent,
    Console
  }
}
