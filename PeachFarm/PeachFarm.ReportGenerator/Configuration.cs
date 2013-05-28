using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace PeachFarm.Reporting.Configuration
{
  public class ReportGeneratorSection : ConfigurationSection
  {
    [ConfigurationProperty(Constants.Controller)]
    public Controller Controller
    {
      get { return (Controller)this[Constants.Controller]; }
      set { this[Constants.Controller] = value; }
    }

		[ConfigurationProperty(Constants.RabbitMq)]
		public RabbitMqElement RabbitMq
		{
			get { return (RabbitMqElement)this[Constants.RabbitMq]; }
			set { this[Constants.RabbitMq] = value; }
		}

		[ConfigurationProperty(Constants.MongoDb)]
		public MongoDbElement MongoDb
		{
			get { return (MongoDbElement)this[Constants.MongoDb]; }
			set { this[Constants.MongoDb] = value; }
		}

		[ConfigurationProperty(Constants.Monitor)]
		public MonitorElement Monitor
		{
			get { return (MonitorElement)this[Constants.Monitor]; }
			set { this[Constants.Monitor] = value; }
		}

		/*
		[ConfigurationProperty(Constants.ReportGenerator)]
		public ReportGeneratorElement ReportGenerator
		{
			get { return (ReportGeneratorElement)this[Constants.ReportGenerator]; }
			set { this[Constants.ReportGenerator] = value; }
		}
		//*/
  }

	/*
	public class ReportGeneratorElement : ConfigurationElement
	{
		[ConfigurationProperty(Constants.ConcurrentJobs)]
		public int ConcurrentJobs
		{
			get { return (int)this[Constants.ConcurrentJobs]; }
			set { this[Constants.ConcurrentJobs] = value; }
		}
	}
	//*/

	public class MonitorElement : ConfigurationElement
	{
		[ConfigurationProperty(Constants.BaseURL)]
		public string BaseURL
		{
			get { return (string)this[Constants.BaseURL]; }
			set { this[Constants.BaseURL] = value; }
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

		[ConfigurationProperty(Constants.SSL)]
		public bool SSL
		{
			get { return (bool)this[Constants.SSL]; }
			set { this[Constants.SSL] = value; }
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

	public class Controller : ConfigurationElement
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

		public const string Monitor = "Monitor";
		public const string BaseURL = "baseURL";

		public const string ReportGenerator = "ReportGenerator";
		public const string ConcurrentJobs = "concurrentJobs";
  }
}
