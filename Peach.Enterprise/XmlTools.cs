using Peach.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Peach.Enterprise
{
	public static class XmlTools
	{
		static Dictionary<Type, XmlSchema> schemas = new Dictionary<Type, XmlSchema>();

		public static XmlSchema GetSchema(Type type)
		{
			var xmlRoot = type.GetAttributes<XmlRootAttribute>(null).FirstOrDefault();
			if (xmlRoot == null)
				throw new ArgumentException("Type '{0}' is missing XmlRootAttribute.".Fmt(type.FullName));

			XmlSchema ret;
			if (schemas.TryGetValue(type, out ret))
				return ret;

			var importer = new XmlReflectionImporter();
			var schema = new XmlSchemas();
			var exporter = new XmlSchemaExporter(schema);

			var xmlTypeMapping = importer.ImportTypeMapping(type);
			exporter.ExportTypeMapping(xmlTypeMapping);

			foreach (XmlSchemaObject obj in schema[0].Items)
			{
				if (obj is XmlSchemaElement)
					continue;

				var asType = (XmlSchemaComplexType)obj;

				foreach (XmlSchemaAttribute attr in asType.Attributes)
				{
					if (attr.DefaultValue == null)
						attr.Use = XmlSchemaUse.Required;
					else
						attr.Use = XmlSchemaUse.Optional;
				}

				if (asType.ContentModel != null)
				{
					var content = (XmlSchemaComplexContent)asType.ContentModel;
					var ext = (XmlSchemaComplexContentExtension)content.Content;

					// If xs:complexType isMixed='true' we need to set
					// the xs:complexContent to have isMixed='true' also
					if (content.IsMixed == false && asType.IsMixed == true)
						content.IsMixed = true;

					foreach (XmlSchemaAttribute attr in ext.Attributes)
					{
						if (attr.DefaultValue == null)
							attr.Use = XmlSchemaUse.Required;
						else
							attr.Use = XmlSchemaUse.Optional;
					}
				}	
			}

			var errors = new StringBuilder();

			ValidationEventHandler handler = (o, e) =>
			{
				var ex = e.Exception;

				errors.AppendFormat("Line: {0}, Position: {1} - ", ex.LineNumber, ex.LinePosition);
				errors.Append(ex.Message);
				errors.AppendLine();
			};

			schema.Compile(handler, false);

			if (errors.Length > 0)
				throw new PeachException("{0} schema failed to generate: \r\n{0}".Fmt(type.Name, errors));

			ret = schema[0];

			schemas.Add(type, ret);

			return ret;
		}

		class Reader<T> : IDisposable
		{
			StringBuilder errors;
			XmlReaderSettings settings;
			XmlParserContext parserCtx;
			XmlReader xmlReader;

			public Reader(string inputUri)
			{
				Initialize();

				xmlReader = XmlReader.Create(inputUri, settings, parserCtx);
			}

			public Reader(Stream stream)
			{
				Initialize();
				
				xmlReader = XmlReader.Create(stream, settings, parserCtx);
			}

			public Reader(TextReader textReader)
			{
				Initialize();

				xmlReader = XmlReader.Create(textReader, settings, parserCtx);
			}

			public void Dispose()
			{
				if (xmlReader != null)
					xmlReader.Close();
			}

			private Exception MakeError(Exception inner, string msg, params object[] args)
			{
				var suffix = msg.Fmt(args);

				if (string.IsNullOrEmpty(xmlReader.BaseURI))
					return new PeachException("Error, {0} file {1}".Fmt(typeof(T).Name, suffix), inner);

				return new PeachException("Error, {0} file '{1}' {2}".Fmt(typeof(T).Name, xmlReader.BaseURI, suffix), inner);
			}

			public T Deserialize()
			{
				try
				{
					var s = new XmlSerializer(typeof(T));
					var o = s.Deserialize(xmlReader);
					var r = (T)o;

					if (errors.Length > 0)
						throw MakeError(null, "failed to validate:\r\n{0}", errors);

					return r;
				}
				catch (InvalidOperationException ex)
				{
					var inner = ex.InnerException as XmlException;
					if (inner != null && !string.IsNullOrEmpty(inner.Message))
						throw MakeError(ex, "failed to load. {0}", inner.Message);
					else
						throw MakeError(ex, "failed to load. {0}", ex.Message);
				}
			}

			void Initialize()
			{
				var schema = GetSchema(typeof(T));

				var schemas = new XmlSchemaSet();
				schemas.Add(schema);

				errors = new StringBuilder();

				settings = new XmlReaderSettings();
				settings.ValidationType = ValidationType.Schema;
				settings.Schemas = schemas;
				settings.NameTable = new NameTable();
				settings.ValidationEventHandler += delegate(object sender, ValidationEventArgs e)
				{
					var ex = e.Exception;

					errors.AppendFormat("Line: {0}, Position: {1} - ", ex.LineNumber, ex.LinePosition);
					errors.Append(ex.Message);
					errors.AppendLine();
				};

				// Default the namespace to the namespace of the XmlRootAttribute
				var nsMgr = new XmlNamespaceManager(settings.NameTable);

				if (schema.TargetNamespace != null)
					nsMgr.AddNamespace("", schema.TargetNamespace);

				parserCtx = new XmlParserContext(settings.NameTable, nsMgr, null, XmlSpace.Default);
			}
		}

		public static T Deserialize<T>(string inputUri)
		{
			using (var rdr = new Reader<T>(inputUri))
			{
				return rdr.Deserialize();
			}
		}

		public static T Deserialize<T>(Stream stream)
		{
			using (var rdr = new Reader<T>(stream))
			{
				return rdr.Deserialize();
			}
		}

		public static T Deserialize<T>(TextReader textReader)
		{
			using (var rdr = new Reader<T>(textReader))
			{
				return rdr.Deserialize();
			}
		}
	}
}
