using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace PeachFarm.Node.Configuration
{
  public class NodeSection : ConfigurationSection
  {
    [ConfigurationProperty(Constants.Controller, IsRequired=true)]
    public Controller Controller
    {
      get { return (Controller)this[Constants.Controller]; }
      set { this[Constants.Controller] = value; }
    }

		[ConfigurationProperty(Constants.Tags, IsRequired = true)]
    public TagsCollection Tags
    {
      get { return (TagsCollection)this[Constants.Tags]; }
      set { this[Constants.Tags] = value; }
    }

		[ConfigurationProperty(Constants.RabbitMq, IsRequired = true)]
		public RabbitMqElement RabbitMq
		{
			get { return (RabbitMqElement)this[Constants.RabbitMq]; }
			set { this[Constants.RabbitMq] = value; }
		}

		public void Validate()
		{
			StringBuilder message = new StringBuilder();

			if (this.Controller == null)
			{
				message.AppendLine(") Missing configuration element in peachfarm.node: <Controller ipAddress=\0.0.0.0\" />");
			}
			else
			{
				if (String.IsNullOrEmpty(this.Controller.IpAddress))
				{
					message.AppendLine(") Controller IP address is required");
				}
			}

			if (this.Tags == null)
			{
				this.Tags = new TagsCollection();
			}

			if (this.RabbitMq == null)
			{
				message.AppendLine(") Missing configuration element in peachfarm.node: <RabbitMq hostName=\"0.0.0.0\" port=\"-1\" userName=\"guest\" password=\"guest\" useSSL=\"false\" />");
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

  public class Controller : ConfigurationElement
  {
		[ConfigurationProperty(Constants.IPAddress, IsRequired = true)]
    public string IpAddress
    {
      get { return (string)this[Constants.IPAddress]; }
      set { this[Constants.IPAddress] = value; }
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

    public override string ToString()
    {
      StringBuilder sb = new StringBuilder();
      foreach (Tag tag in this)
      {
        sb.Append(tag.Name + ",");
      }
      sb.Remove(sb.Length - 1, 1);
      return sb.ToString();
    }
  }

  public class Tag : ConfigurationElement
  {
		[ConfigurationProperty(Constants.Name, IsRequired = true)]
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

		public const string RabbitMq = "RabbitMq";
		public const string HostName = "hostName";
		public const string Port = "port";
		public const string UserName = "userName";
		public const string Password = "password";
		public const string SSL = "useSSL";

    public const string Tags = "Tags";
    public const string Tag = "Tag";
    public const string Name = "name";
  }
}
