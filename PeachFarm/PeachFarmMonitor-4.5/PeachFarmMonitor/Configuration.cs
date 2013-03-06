using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace PeachFarmMonitor.Configuration
{
  public class PeachFarmMonitorSection : ConfigurationSection
  {
    [ConfigurationProperty(Constants.MongoDb)]
    public MongoDbElement MongoDb
    {
      get { return (MongoDbElement)this[Constants.MongoDb]; }
      set { this[Constants.MongoDb] = value; }
    }
  }

  public class MongoDbElement : ConfigurationElement
  {
    [ConfigurationProperty(Constants.ConnectionString)]
    public string ConnectionString
    {
      get { return (string)this[Constants.ConnectionString]; }
      set { this[Constants.ConnectionString] = value; }
    }
  }

  public static class Constants
  {
    public const string MongoDb = "MongoDb";
    public const string ConnectionString = "connectionString";
  }
}