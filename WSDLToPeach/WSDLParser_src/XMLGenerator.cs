//Author contact: Thanh.Dao@gmx.netusing System;
using System.IO;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Schema;

namespace WSDLParser
{
	/// <summary>
	/// Summary description for XMLGenerator.
	/// </summary>
	public class XMLGenerator
	{
		public XMLGenerator()
		{
		}

		public static string GetXmlSample(TreeNode node)
		{
			StringWriter sw=new StringWriter ();
			XmlTextWriter writer=new XmlTextWriter (sw);
			writer.Formatting=Formatting.Indented;
			writer.WriteStartDocument ();					
			prefIndex=-1;
			WriteNode(writer, node);
			
			writer.Close() ;
			
			return sw.ToString() ;
		}

		static int prefIndex=0;

		private static  void WriteNode(XmlTextWriter writer, TreeNode node)
		{
			SchemaParser.NodeData data=null;
			if (node.Tag != null)
			{
				data=node.Tag as SchemaParser.NodeData ;
			}
			
			if (data == null) return;			
			
			if (data.baseObj is XmlSchemaElement
				|| data.baseObj is XmlSchemaComplexType
				|| data.baseObj is XmlSchemaSimpleType
				)
			{				
				
				
				

				//if (pref != null)  pref + ":" + node.Text;
				string name=node.Text;
				if (name.LastIndexOf(":") != -1)
					name=name.Substring(name.LastIndexOf(":") + 1, name.Length - name.LastIndexOf(":") - 1) ;

				if (data.Namespace != string.Empty )
				{
					++prefIndex;
					string pref=writer.LookupPrefix (data.Namespace);
					
					if (pref == null) pref="m"+prefIndex;

					writer.WriteStartElement(pref, name, data.Namespace) ;					
				}
				else
					writer.WriteStartElement(name) ;
				
				bool ct=false;
				if (node.Nodes.Count > 0)
				{
					foreach (TreeNode child in node.Nodes)
					{						
						if (child.Tag is SchemaParser.NodeData)
						{
							if (((SchemaParser.NodeData)child.Tag).baseObj is XmlSchemaAttribute)
								writer.WriteAttributeString(child.Text, ((SchemaParser.NodeData)child.Tag).Type );
						}						
					}

					foreach (TreeNode child in node.Nodes)
					if (child.Tag is SchemaParser.NodeData)
					if (!(((SchemaParser.NodeData)child.Tag).baseObj is XmlSchemaAttribute))
					{
						ct=true;
						WriteNode(writer, child);
					}
				};
				
				if (!ct)writer.WriteString (data.Type);

				writer.WriteEndElement();
				
			} 
		}
	}
}
