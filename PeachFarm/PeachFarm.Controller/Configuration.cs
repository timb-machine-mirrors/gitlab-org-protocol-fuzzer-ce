using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace PeachFarm.Controller.Configuration
{
  public class ControllerSection : ConfigurationSection
  {
		//*
		[ConfigurationProperty(Constants.Controller, IsRequired = false)]
    public Controller Controller
    {
      get { return (Controller)this[Constants.Controller]; }
      set { this[Constants.Controller] = value; }
    }
		//*/
		[ConfigurationProperty(Constants.MongoDb, IsRequired = false)]
    public MongoDbElement MongoDb
    {
      get { return (MongoDbElement)this[Constants.MongoDb]; }
      set { this[Constants.MongoDb] = value; }
    }

		[ConfigurationProperty(Constants.RabbitMq, IsRequired = false)]
    public RabbitMqElement RabbitMq
    {
      get { return (RabbitMqElement)this[Constants.RabbitMq]; }
      set { this[Constants.RabbitMq] = value; }
    }

		public void Validate()
		{
			StringBuilder message = new StringBuilder();

			if (String.IsNullOrEmpty(this.MongoDb.ConnectionString))
			{
				message.AppendLine(") MongoDB Connection String is required.");
				message.AppendLine("\t<MongoDb connectionString=\"mongodb://0.0.0.0/?safe=true\" />");
			}

			if (String.IsNullOrEmpty(this.RabbitMq.HostName))
			{
				message.AppendLine(") RabbitMQ host name is required");
				message.AppendLine(
					"\t<RabbitMq hostName=\"0.0.0.0\" port=\"-1\" userName=\"guest\" password=\"guest\" useSSL=\"false\" />");
			}

			if (message.Length > 0)
			{
				message.Insert(0, "Errors found in application config file: \n");
				throw new ApplicationException(message.ToString());
			}
		}
  }

  public class MongoDbElement : ConfigurationElement
  {
		[ConfigurationProperty(Constants.ConnectionString, IsRequired = false)]
    public string ConnectionString
    {
      get { return (string)this[Constants.ConnectionString]; }
      set { this[Constants.ConnectionString] = value; }
    }
  }

  public class RabbitMqElement : ConfigurationElement
  {
		[ConfigurationProperty(Constants.HostName, IsRequired = false)]
    public string HostName
    {
      get { return (string)this[Constants.HostName]; }
      set { this[Constants.HostName] = value; }
    }

		[ConfigurationProperty(Constants.Port, IsRequired = false)]
    public int Port
    {
      get { return (int)this[Constants.Port]; }
      set { this[Constants.Port] = value; }
    }

		[ConfigurationProperty(Constants.UserName, IsRequired = false)]
    public string UserName
    {
      get { return (string)this[Constants.UserName]; }
      set { this[Constants.UserName] = value; }
    }

		[ConfigurationProperty(Constants.Password, IsRequired = false)]
    public string Password
    {
      get { return (string)this[Constants.Password]; }
      set { this[Constants.Password] = value; }
    }

		[ConfigurationProperty(Constants.SSL, IsRequired = false)]
		public bool SSL
		{
			get { return (bool)this[Constants.SSL]; }
			set { this[Constants.SSL] = value; }
		}
	}

  public class Controller : ConfigurationElement
  {
		[ConfigurationProperty(Constants.Name, IsRequired = false)]
    public string Name
    {
      get { return (string)this[Constants.Name]; }
      set { this[Constants.Name] = value; }
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
		public const string SSL = "useSSL";

    public const string Controller = "Controller";
    public const string Name = "nameOverride";
  }
}
