using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace PeachFarm.Controller.Configuration
{
  public class ServerSection : ConfigurationSection
  {
    [ConfigurationProperty(Constants.Output)]
    public Output Output
    {
      get { return (Output)this[Constants.Output]; }
      set { this[Constants.Output] = value; }
    }

    [ConfigurationProperty(Constants.ServerHost)]
    public ServerHost ServerHost
    {
      get { return (ServerHost)this[Constants.ServerHost]; }
      set { this[Constants.ServerHost] = value; }
    }

    [ConfigurationProperty(Constants.MongoDb)]
    public MongoDbElement MongoDb
    {
      get { return (MongoDbElement)this[Constants.MongoDb]; }
      set { this[Constants.MongoDb] = value; }
    }

    [ConfigurationProperty(Constants.RabbitMq)]
    public RabbitMqElement RabbitMq
    {
      get { return (RabbitMqElement)this[Constants.RabbitMq]; }
      set { this[Constants.RabbitMq] = value; }
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

  public class RabbitMqElement : ConfigurationElement
  {
    [ConfigurationProperty(Constants.HostName)]
    public string HostName
    {
      get { return (string)this[Constants.HostName]; }
      set { this[Constants.HostName] = value; }
    }

    [ConfigurationProperty(Constants.Port)]
    public int Port
    {
      get { return (int)this[Constants.Port]; }
      set { this[Constants.Port] = value; }
    }

    [ConfigurationProperty(Constants.UserName)]
    public string UserName
    {
      get { return (string)this[Constants.UserName]; }
      set { this[Constants.UserName] = value; }
    }

    [ConfigurationProperty(Constants.Password)]
    public string Password
    {
      get { return (string)this[Constants.Password]; }
      set { this[Constants.Password] = value; }
    }
  }

  public class Output : ConfigurationElement
  {
    [ConfigurationProperty(Constants.OutputType)]
    public OutputType OutputType
    {
      get{return (OutputType)this[Constants.OutputType];}
      set { this[Constants.OutputType] = value; }
    }
  }

  public class ServerHost : ConfigurationElement
  {
    [ConfigurationProperty(Constants.IpAddress)]
    public string IpAddress
    {
      get { return (string)this[Constants.IpAddress]; }
      set { this[Constants.IpAddress] = value; }
    }
  }

  public static class Constants
  {
    public const string MongoDb = "MongoDb";
    public const string ConnectionString = "connectionString";

    public const string RabbitMq = "RabbitMq";
    public const string HostName = "hostName";
    public const string Port = "port";
    public const string UserName = "userName";
    public const string Password = "password";

    public const string Output = "Output";
    public const string OutputType = "type";

    public const string ServerHost = "ServerHost";
    public const string IpAddress = "ipAddress";
  }

  public enum OutputType
  {
    Silent,
    Console
  }
}
