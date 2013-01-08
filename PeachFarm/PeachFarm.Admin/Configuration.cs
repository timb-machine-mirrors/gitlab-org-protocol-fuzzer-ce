using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace PeachFarm.Admin.Configuration
{
  public class AdminSection : ConfigurationSection
  {
    [ConfigurationProperty(Constants.Controller)]
    public Controller Controller
    {
      get { return (Controller)this[Constants.Controller]; }
      set { this[Constants.Controller] = value; }
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

  }
}
