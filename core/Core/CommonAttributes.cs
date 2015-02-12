﻿using System;
using System.Collections.Generic;

namespace Peach.Core
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
	public class ParameterAttribute : Attribute
	{
		public string name { get; private set; }
		public Type type { get; private set; }
		public string description { get; private set; }
		public bool required { get; private set; }
		public string defaultValue { get; private set; }

		/// <summary>
		/// Constructs a REQUIRED parameter.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="type"></param>
		/// <param name="description"></param>
		public ParameterAttribute(string name, Type type, string description)
		{
			this.name = name;
			this.type = type;
			this.description = description;
			this.required = true;
			this.defaultValue = null;
		}

		/// <summary>
		/// Constructs an OPTIONAL parameter.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="type"></param>
		/// <param name="description"></param>
		/// <param name="defaultValue"></param>
		public ParameterAttribute(string name, Type type, string description, string defaultValue)
		{
			if (defaultValue == null)
				throw new ArgumentNullException("defaultValue");

			this.name = name;
			this.type = type;
			this.description = description;
			this.required = false;
			this.defaultValue = defaultValue;
		}
	}

	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
	public abstract class PluginAttribute : Attribute
	{
		#region Obsolete Functions

		[Obsolete("This property is obsolete and has been replaced by the Internal property.")]
		public bool IsTest
		{
			get { return Internal; }
			set { Internal = value; }
		}

		#endregion

		/// <summary>
		/// The name of the plugin.
		/// </summary>
		public string Name { get; private set; }

		/// <summary>
		/// The type of the plugin.
		/// </summary>
		public Type Type { get; private set; }

		/// <summary>
		/// Is this the default name for the plugin.
		/// For plugins with multiple names, the default name will be used in the schema.
		/// </summary>
		public bool IsDefault { get; private set; }

		/// <summary>
		/// Is this an internal plugin.
		/// Internal plugins are omitted from schema generation.
		/// </summary>
		public bool Internal { get; set; }

		/// <summary>
		/// The operating systems that support this plugin.
		/// </summary>
		public Platform.OS OS { get; set; }

		protected PluginAttribute(Type type, string name, bool isDefault)
		{
			Name = name;
			Type = type;
			IsDefault = isDefault;
			OS = Platform.OS.All;
		}
	}

	[AttributeUsageAttribute(AttributeTargets.Property | AttributeTargets.Field)]
	public class PluginElementAttribute : Attribute
	{
		public bool Named { get; set; }
		public bool Combine { get; set; }
		public string ElementName { get; private set; }
		public string AttributeName { get; private set; }
		public Type PluginType { get; private set; }

		public PluginElementAttribute(string elementName, string attributeName, Type pluginType)
		{
			ElementName = elementName;
			AttributeName = attributeName;
			PluginType = pluginType;
		}

		public PluginElementAttribute(string attributeName, Type pluginType)
		{
			ElementName = pluginType.Name;
			AttributeName = attributeName;
			PluginType = pluginType;
		}
	}

	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
	public class AliasAttribute : Attribute
	{
		public string Name { get; set; }

		public AliasAttribute(string name)
		{
			Name = name;
		}
	}
}
