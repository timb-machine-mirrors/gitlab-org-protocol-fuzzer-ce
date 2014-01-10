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

		//[ConfigurationProperty(Constants.MySql)]
		//public MySqlElement MySql
		//{
		//	get { return (MySqlElement)this[Constants.MySql]; }
		//	set { this[Constants.MySql] = value; }
		//}

		public void Validate()
		{
			StringBuilder message = new StringBuilder();

			if (String.IsNullOrEmpty(this.Controller.IpAddress))
			{
				message.AppendLine(") Controller IP address or overridden name is required.");
				message.AppendLine("\t<Controller ipAddress=\"0.0.0.0\" />");
			}


			if (String.IsNullOrEmpty(this.MongoDb.ConnectionString))
			{
				message.AppendLine(") MongoDB Connection String is required.");
				message.AppendLine("\t<MongoDb connectionString=\"mongodb://0.0.0.0/?safe=true\" />");
			}

			//if (String.IsNullOrEmpty(this.MySql.ConnectionString))
			//{
			//	message.AppendLine(") MySql Connection String is required.");
			//	message.AppendLine("\t<MySql connectionString=\"Server=myServerAddress;Port=1234;Database=myDataBase;Uid=myUsername;Pwd=myPassword;\" />");
			//}

			if (String.IsNullOrEmpty(this.RabbitMq.HostName))
			{
				message.AppendLine(") RabbitMQ host name is required");
				message.AppendLine(
					"\t<RabbitMq hostName=\"0.0.0.0\" port=\"-1\" userName=\"guest\" password=\"guest\" useSSL=\"false\" />");
			}

			if (String.IsNullOrEmpty(this.Monitor.BaseURL))
			{
				message.AppendLine(") Monitor baseUrl is required");
				message.AppendLine(
					"\t<Monitor baseurl=\"http://host/vfolder\" />");
			}
			else
			{
				if (this.Monitor.BaseURL.EndsWith("/") == false)
				{
					this.Monitor.BaseURL = this.Monitor.BaseURL + "/";
				}
			}

			if (message.Length > 0)
			{
				message.Insert(0, "Errors found in application config file: \n");
				throw new ApplicationException(message.ToString());
			}
		}
  }

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

	public class MySqlElement : ConfigurationElement
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

		public const string MySql = "MySql";
  }
}
