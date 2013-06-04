﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace PeachFarm.Admin.Configuration
{
  public class AdminSection : ConfigurationSection
  {
		[ConfigurationProperty(Constants.Controller, IsRequired = true)]
    public Controller Controller
    {
      get { return (Controller)this[Constants.Controller]; }
      set { this[Constants.Controller] = value; }
    }

		[ConfigurationProperty(Constants.RabbitMq, IsRequired = true)]
		public RabbitMqElement RabbitMq
		{
			get { return (RabbitMqElement)this[Constants.RabbitMq]; }
			set { this[Constants.RabbitMq] = value; }
		}

		[ConfigurationProperty(Constants.MongoDb, IsRequired = true)]
		public MongoDbElement MongoDb
		{
			get { return (MongoDbElement)this[Constants.MongoDb]; }
			set { this[Constants.MongoDb] = value; }
		}

		public void Validate()
		{
			StringBuilder message = new StringBuilder();

			if (this.Controller == null)
			{
				message.AppendLine(") Missing configuration element in peachfarm.admin: <Controller ipAddress=\0.0.0.0\" />");
			}
			else
			{
				if (String.IsNullOrEmpty(this.Controller.IpAddress))
				{
					message.AppendLine(") Controller IP address is required");
				}
			}

			if (this.MongoDb == null)
			{
				message.AppendLine(") Missing configuration element in peachfarm.admin: <MongoDb connectionString=\"mongodb://0.0.0.0/?safe=true\" />");
			}
			else
			{
				if (String.IsNullOrEmpty(this.MongoDb.ConnectionString))
				{
					message.AppendLine(") MongoDB connection string is required");
				}
			}

			if (this.RabbitMq == null)
			{
				message.AppendLine(") Missing configuration element in peachfarm.admin: <RabbitMq hostName=\"0.0.0.0\" port=\"-1\" userName=\"guest\" password=\"guest\" useSSL=\"false\" />");
			}
			else
			{
				if (String.IsNullOrEmpty(this.RabbitMq.HostName))
				{
					message.AppendLine(") RabbitMQ host name is required");
				}
			}

			if (message.Length > 0)
			{
				message.Insert(0, "Errors found in application config file: \n");
				throw new ApplicationException(message.ToString());
			}
		}

  }

	public class RabbitMqElement : ConfigurationElement
	{
		[ConfigurationProperty(Constants.HostName, IsRequired = true)]
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

		[ConfigurationProperty(Constants.SSL)]
		public bool SSL
		{
			get { return (bool)this[Constants.SSL]; }
			set { this[Constants.SSL] = value; }
		}
	}

	public class MongoDbElement : ConfigurationElement
	{
		[ConfigurationProperty(Constants.ConnectionString, IsRequired = true)]
		public string ConnectionString
		{
			get { return (string)this[Constants.ConnectionString]; }
			set { this[Constants.ConnectionString] = value; }
		}
	}

	public class Controller : ConfigurationElement
	{
		[ConfigurationProperty(Constants.IpAddress, IsRequired = true)]
		public string IpAddress
		{
			get { return (string)this[Constants.IpAddress]; }
			set { this[Constants.IpAddress] = value; }
		}
	}

  public static class Constants
  {
    public const string Controller = "Controller";
		public const string IpAddress = "ipAddress";

		public const string RabbitMq = "RabbitMq";
		public const string HostName = "hostName";
		public const string Port = "port";
		public const string UserName = "userName";
		public const string Password = "password";
		public const string SSL = "useSSL";

		public const string MongoDb = "MongoDb";
		public const string ConnectionString = "connectionString";
  }
}
