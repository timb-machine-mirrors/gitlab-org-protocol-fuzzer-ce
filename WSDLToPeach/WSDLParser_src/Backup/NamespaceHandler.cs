using System;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace WSDLParser
{
	/// <summary>
	/// Summary description for NamespaceHandler.
	/// </summary>
	internal class NamespaceHandler
	{
			
		public NamespaceHandler()
		{
		}


		internal static string LoopupPrefix(XmlSerializerNamespaces namespaces, string ns)
		{
			if (ns == string.Empty ) return string.Empty;
			
			XmlQualifiedName[] XmlQualifiedNameList =namespaces.ToArray();
			
			foreach (XmlQualifiedName XmlQualifiedName in XmlQualifiedNameList )
			{
				if (!XmlQualifiedName.IsEmpty && XmlQualifiedName.Namespace.Equals(ns) )
				{
					return  XmlQualifiedName.Name;
				}
			}

			return string.Empty;
		}
				
		internal static string LoopupNamespace(XmlSerializerNamespaces namespaces,string prefix)
		{
			if (prefix != string.Empty) return string.Empty;
			
			XmlQualifiedName[] XmlQualifiedNameList =namespaces.ToArray();
			foreach (XmlQualifiedName XmlQualifiedName in XmlQualifiedNameList )
			{
				if (!XmlQualifiedName.IsEmpty && XmlQualifiedName.Name.Equals(prefix) )
				{
					return  XmlQualifiedName.Namespace;
				}
			}

			return string.Empty;
		}



	}
}
