//Author contact: Thanh.Dao@gmx.net

using System;
using System.Collections;
using System.Reflection;
using System.Threading;
using System.Web.Services.Description;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace WSDLParser
{
	#region Denote Prefix for long schem name
	//Denote prefix for long name data type
	//Simply later by I2
	using QName = XmlQualifiedName; 
	using Form = XmlSchemaForm; 
	using Use = XmlSchemaUse; 
	using SOM=XmlSchema;
	using SOMList = XmlSchemaObjectCollection; 
	using SOMObject = XmlSchemaObject; 
	using Element = XmlSchemaElement; 
	using Attr = XmlSchemaAttribute; 
	using AttrGroup = XmlSchemaAttributeGroup; 
	using AttrGroupRef = XmlSchemaAttributeGroupRef; 
	using SimpleType = XmlSchemaSimpleType; 
	using ComplexType = XmlSchemaComplexType; 
	using SimpleModel = XmlSchemaSimpleContent; 
	using SimpleExt = XmlSchemaSimpleContentExtension; 
	using SimpleRst = XmlSchemaSimpleContentRestriction; 
	using ComplexModel = XmlSchemaComplexContent; 
	using ComplexExt = XmlSchemaComplexContentExtension; 
	using ComplexRst = XmlSchemaComplexContentRestriction; 
	using SimpleTypeRst = XmlSchemaSimpleTypeRestriction; 
	using SimpleTypeModel = XmlSchemaSimpleTypeContent;
	using SimpleList = XmlSchemaSimpleTypeList; 
	using SimpleUnion = XmlSchemaSimpleTypeUnion; 
	using SchemaFacet = XmlSchemaFacet; 
	using EnumerationFacet = XmlSchemaEnumerationFacet; 
	using MinExcludesiveFacet=XmlSchemaMinExclusiveFacet ;
	using LengthFacet = XmlSchemaLengthFacet; 
	using MinLengthFacet = XmlSchemaMinLengthFacet; 
	using PatternFacet = XmlSchemaPatternFacet;
	using Particle = XmlSchemaParticle; 
	using Sequence = XmlSchemaSequence; 
	using Choice = XmlSchemaChoice;
	using Annotation=XmlSchemaAnnotation;
	using Documentation=XmlSchemaDocumentation;
	using AnyAttribute=XmlSchemaAnyAttribute;
	using AnyElement=XmlSchemaAny;
	using GroupRef=XmlSchemaGroupRef;
	using Group=XmlSchemaGroup;
	using All=XmlSchemaAll ;
	using Include=XmlSchemaInclude;
	#endregion

	/// <summary>
	/// Summary description for SchemaParser.
	/// </summary>	
	
	public class SchemaParser
	{					
		public class NodeData
		{
			public XmlQualifiedName Name;
			public string Type;
			public string Namespace;
			public bool Qualified;
			public XmlSchemaObject baseObj;

			public NodeData (XmlSchemaObject obj, XmlQualifiedName name, string type) 
			{ baseObj=obj; Name=name; Type=type; Namespace=name.Namespace; 			  
			}

		}			

		protected XmlSchemas _schemas;
		public static int TimeOver=2000;

		private static readonly XmlQualifiedName arrayType=new XmlQualifiedName ("Array","http://schemas.xmlsoap.org/soap/encoding/");
		private static readonly XmlQualifiedName arrayTypeRefName=new XmlQualifiedName ("arrayType","http://schemas.xmlsoap.org/soap/encoding/");

		XmlSchemaElement anyElement;
		public SoapBindingUse BindingUse=SoapBindingUse.Literal;		
		ArrayList _queue;
		

		class Encoded
		{
			public string Namespace;
			public XmlSchemaElement Element;
			public Encoded (string ns, XmlSchemaElement elem) 
			{ Namespace=ns; Element=elem; }
		}

		void MyInit()
		{			
			anyElement=new XmlSchemaElement ();
			anyElement.Name="any";
			anyElement.SchemaTypeName=new XmlQualifiedName ("anyType",XmlSchema.Namespace);
			_queue=new ArrayList ();
			 
		}
		
		public SchemaParser (XmlSchemas schemas)
		{
			MyInit ();
			this._schemas=schemas;			
			PrefixProcess();
		}
		
		public TreeNode Translate (XmlQualifiedName qname)
		{
			processHasExited=false;
			_node=new TreeNode() ;
			_qname=qname;
			StartParsing();

			ThreadStart threadStart=new ThreadStart(StartParsing);
			Thread thread=new Thread(threadStart);				
			
			thread.Start();		
							
			thread.Join(TimeOver) ;

			return _node;
		}

		XmlSerializerNamespaces _namespaces=new XmlSerializerNamespaces() ;

		void PrefixProcess()
		{
			_namespaces=new XmlSerializerNamespaces();
			for(int i=0; i < _schemas.Count; i++)
			{						
				XmlSchema schema=_schemas[i];
				
				if (schema.TargetNamespace != string.Empty && NamespaceHandler.LoopupPrefix(_namespaces, schema.TargetNamespace) == string.Empty)
				{
					_namespaces.Add("m" + i, schema.TargetNamespace) ;
				}

			}

			for(int i=0; i < _schemas.Count; i++)
			{
				XmlSchema schema=_schemas[i];
				XmlQualifiedName[] XmlQualifiedNameList=schema.Namespaces.ToArray();					
									
				XmlSerializerNamespaces xmlns=new XmlSerializerNamespaces() ;	

				int j=0;
				foreach (XmlQualifiedName qname in XmlQualifiedNameList )
				{
					//if (XmlQualifiedName.Name == string.Empty )
					{
						++j;
						if (qname.Namespace == schema.TargetNamespace)
						{
							string prefix=NamespaceHandler.LoopupPrefix(_namespaces, schema.TargetNamespace);

							if (prefix != string.Empty)
							{
								xmlns.Add(prefix, qname.Namespace) ;
							}
							else
							{
								xmlns.Add("tns", qname.Namespace) ;
							}
						}
						else
						{
							string prefix=NamespaceHandler.LoopupPrefix(_namespaces, schema.TargetNamespace);

							if (prefix != string.Empty)
							{
								xmlns.Add(prefix, qname.Namespace) ;
							}
							else
							{
								xmlns.Add("ns"+j, qname.Namespace) ;
							}
						}
					}
					
					//	xmlns.Add(XmlQualifiedName.Name, XmlQualifiedName.Namespace) ; 

				}
				schema.Namespaces = xmlns ;
			}
		}

		XmlQualifiedName _qname;
		TreeNode _node;
		bool processHasExited=false;

		void StartParsing()
		{
			try
			{					
				processHasExited=false;
				object lookup=_schemas.Find(_qname, typeof(XmlSchemaElement)) ;

				if (lookup != null)
					Create_RootElement (_node, _qname);	
				else
					Create_CustomType (_node, _qname);

				processHasExited=true;
			}
			catch(Exception e)
			{
				//MessageBox.Show(e.Message.ToString() ) ;
			}			
		}
				
		
		void Create_RootElement (TreeNode inner, XmlQualifiedName qname)
		{
			XmlSchemaElement elem=(XmlSchemaElement) _schemas.Find (qname, typeof(XmlSchemaElement));
			if (elem == null) throw new Exception ("Element not found: " + qname);
			Add_Element (inner, qname.Namespace, elem);
		}
		
		private bool IsInfiniteRecursiveLoop(TreeNode parent, XmlQualifiedName qname)
		{
			if (qname.IsEmpty) return false;

			TreeNode ancestor=parent;
			int count=0;
			while (ancestor != null)
			{				
				if (ancestor.Tag != null && ancestor.Tag is NodeData) 
				{
					XmlQualifiedName aqname=((NodeData) ancestor.Tag).Name ;
					if (aqname.Equals(qname)  )					
						++count;					
				}
				ancestor=ancestor.Parent;
			}

			if (count > 1)
			{
				int aaa=0;
			}

			return count > 1;
		}

		XmlSchema FindSchema(string targetNamespace)
		{
			for(int i=0; i < _schemas.Count; i++)
				if (_schemas[i].TargetNamespace == targetNamespace)
				{						
					return _schemas[i];					
				}
			return null;
		}


		//string Get
		void Add_Element (TreeNode complexNode, string ns, XmlSchemaElement elem)
		{
			//add to complexNode
			XmlQualifiedName nodeName=XmlQualifiedName.Empty;
			
			if (!elem.RefName.IsEmpty) 
			{
				XmlSchemaElement refElem=GetRefElement (elem);
				if (refElem == null) throw new Exception ("Global element not found: " + elem.RefName);
				nodeName=elem.RefName;
				elem=refElem;

			}
			else
			{				
				nodeName=elem.QualifiedName;
			}
			
			if (IsInfiniteRecursiveLoop (complexNode, nodeName)) return;

			TreeNode elemNode=new TreeNode(nodeName.Name) ;
			if (nodeName.Namespace != string.Empty)
			{
				elemNode.Text=NamespaceHandler.LoopupPrefix(_namespaces, nodeName.Namespace ) + ":" + nodeName.Name ;
			}			
			
			elemNode.Tag=nodeName;
			complexNode.Nodes.Add(elemNode) ;

			NodeData data=new NodeData(elem, nodeName, "") ;				
			elemNode.Tag=data;

			if (!elem.SchemaTypeName.IsEmpty)
			{
				XmlSchemaComplexType ct=GetComplexTypeByName (elem.SchemaTypeName);
				

				if (ct != null)
				{
					data.Type=ct.QualifiedName.Name;

					Parse_ComplexType (elemNode, ct, nodeName);						
				}
				else
					if (!elem.SchemaTypeName.IsEmpty)
				{																		
					data.Type=GetBuiltInTypeName (elem.SchemaTypeName);
					string test="";
					if (BindingUse == SoapBindingUse.Encoded) 
						 test="type"  + GetQualifiedNameString (complexNode, elem.SchemaTypeName)
							+( (GetBuiltInTypeName (elem.SchemaTypeName)));						
				}
			}
			else if (elem.SchemaType == null)
			{
				data.Type="any";
			}
			else
			{
				if (elem.SchemaType is XmlSchemaComplexType) 
					Parse_ComplexType (elemNode, (XmlSchemaComplexType) elem.SchemaType, nodeName);
				else
					if (elem.SchemaType is XmlSchemaSimpleType)
					{
						XmlSchemaSimpleType st=elem.SchemaType as XmlSchemaSimpleType;
						data.Type=GetBuiltInTypeName (st.QualifiedName );						
					}
			}


			
		}
		
		void Create_CustomType (TreeNode inner, XmlQualifiedName qname)
		{
			XmlSchemaComplexType ctype=GetComplexTypeByName (qname);
			TreeNode node=new TreeNode() ;			
			inner.Nodes.Add(node) ;
			node.Text=qname.Name;

			if (ctype != null) 
			{
				Parse_ComplexType (node, ctype, qname);
				return;
			}
				
			XmlSchemaSimpleType stype=(XmlSchemaSimpleType) _schemas.Find (qname, typeof(XmlSchemaSimpleType));
			if (stype != null) 
			{
				Parse_SimpleType (node, stype);
				return;
			}
			
			inner.Tag=((GetBuiltInTypeName (qname)));

			throw new Exception ("Type not found: " + qname);
		}
		
		void Parse_ComplexType (TreeNode inner, XmlSchemaComplexType stype, XmlQualifiedName rootName)
		{			
			Parse_ComplexType (inner, stype, rootName, -1);
		}
		
		void Parse_ComplexType (TreeNode innerNode, XmlSchemaComplexType stype, XmlQualifiedName rootName, int id)
		{
			string ns=rootName.Namespace;
			
			if (rootName.Name.IndexOf ("[]") != -1) rootName=arrayType;
			
			if (BindingUse == SoapBindingUse.Encoded) 
			{
				innerNode.Text=NamespaceHandler.LoopupPrefix(_namespaces, rootName.Namespace ) + ":" + rootName.Name ;
				//innerNode.Text = rootName.Namespace + ":" + rootName.Name;
				ns=string.Empty;
			}
			else
			{	
				if (rootName.Namespace != string.Empty)
					innerNode.Text=NamespaceHandler.LoopupPrefix(_namespaces, rootName.Namespace ) + ":" + rootName.Name ;
				else
					innerNode.Text=rootName.Name;
			}
		
			if (innerNode.Tag == null)
			{
				NodeData data=new NodeData(stype, rootName, "") ;
				innerNode.Tag=data;
			}
			
			if (id != -1)
			{

				TreeNode node=new TreeNode("id:" + id) ;
				innerNode.Nodes.Add(node) ;

				if (rootName != arrayType)
				{
					TreeNode node2=new TreeNode("Type" + GetQualifiedNameString (innerNode, rootName)) ;
					node.Nodes.Add(node2) ;
				}
			}
			
			Add_ComplexAttributes (innerNode, stype);
			Add_ComplexElements (innerNode, ns, stype);			
		}
		
		void Add_ComplexAttributes (TreeNode inner, XmlSchemaComplexType stype)
		{
			Add_Attributes (inner, stype.Attributes, stype.AnyAttribute);
		}
		
		void Add_ComplexElements (TreeNode complexNode, string ns, XmlSchemaComplexType stype)
		{
			if (stype.ContentTypeParticle != null)
			{
				Parse_ParticleComplexContent (complexNode, ns, stype.ContentTypeParticle);
			}
			else
				if (stype.Particle != null)
			{
				Parse_ParticleComplexContent (complexNode, ns, stype.Particle);
			}
			else
			{
				if (stype.ContentModel is XmlSchemaSimpleContent)
					Parse_SimpleContent (complexNode, (XmlSchemaSimpleContent)stype.ContentModel);
				else if (stype.ContentModel is XmlSchemaComplexContent)
					Parse_ComplexContent (complexNode, ns, (XmlSchemaComplexContent)stype.ContentModel);
			}
		}

		void Add_Attributes (TreeNode innerNode, XmlSchemaObjectCollection atts, XmlSchemaAnyAttribute anyat)
		{
			foreach (XmlSchemaObject at in atts)
			{
				if (at is XmlSchemaAttribute)
				{						
					XmlSchemaAttribute attr=(XmlSchemaAttribute)at;
					XmlSchemaAttribute refAttr=attr;											
					
					if (!attr.RefName.IsEmpty) 
					{
						refAttr=GetRefAttribute (attr.RefName);
						if (refAttr == null) throw new Exception ("Global attribute not found: " + attr.RefName);
					}
					
					string type;
					if (!refAttr.SchemaTypeName.IsEmpty) type=GetBuiltInTypeName (refAttr.SchemaTypeName);
					else type=GetBuiltInType (refAttr.SchemaType);
					
					TreeNode node=new TreeNode(refAttr.Name) ;

					if (refAttr.QualifiedName.Namespace != string.Empty)
					{
						node.Text=NamespaceHandler.LoopupPrefix(_namespaces, refAttr.QualifiedName.Namespace ) + ":" + refAttr.Name ;
					}			

					NodeData data=new NodeData(at, refAttr.QualifiedName, type) ;
					node.Tag=data;

					innerNode.Nodes.Add(node) ;						
						
				}
				else if (at is XmlSchemaAttributeGroupRef)
				{
					XmlSchemaAttributeGroupRef gref=(XmlSchemaAttributeGroupRef)at;
					XmlSchemaAttributeGroup grp=(XmlSchemaAttributeGroup) _schemas.Find (gref.RefName, typeof(XmlSchemaAttributeGroup));
					Add_Attributes (innerNode, grp.Attributes, grp.AnyAttribute);
				}
			}
			
			if (anyat != null)
			{
				TreeNode node=new TreeNode("any custom-attribute") ;
				innerNode.Nodes.Add(node) ;				
			}
		}
		
		void Parse_ParticleComplexContent (TreeNode complexNode, string ns, XmlSchemaParticle particle)
		{
			Parse_ParticleContent (complexNode, ns, particle, false);
		}
		
		void Parse_ParticleContent (TreeNode complexNode, string ns, XmlSchemaParticle particle, bool multiValue)
		{
			if (particle is XmlSchemaGroupRef)
				particle=GetRefGroupParticle ((XmlSchemaGroupRef)particle);

			if (particle.MaxOccurs > 1) multiValue=true;
			
			if (particle is XmlSchemaSequence) 
			{
				Parse_SequenceContent (complexNode, ns, ((XmlSchemaSequence)particle).Items, multiValue);
			}
			else if (particle is XmlSchemaChoice) 
			{
				if (((XmlSchemaChoice)particle).Items.Count == 1)
					Parse_SequenceContent (complexNode, ns, ((XmlSchemaChoice)particle).Items, multiValue);
				else
					Parse_ChoiceContent (complexNode, ns, (XmlSchemaChoice)particle, multiValue);
			}
			else if (particle is XmlSchemaAll) 
			{
				Parse_SequenceContent (complexNode, ns, ((XmlSchemaAll)particle).Items, multiValue);
			}
		}

		void Parse_SequenceContent (TreeNode complexNode, string ns, XmlSchemaObjectCollection items, bool multiValue)
		{
			foreach (XmlSchemaObject item in items)
				Add_Item (complexNode, ns, item, multiValue);
		}
		
		void Add_Item (TreeNode complexNode, string ns, XmlSchemaObject item, bool multiValue)
		{
			if (item is XmlSchemaGroupRef)
				item=GetRefGroupParticle ((XmlSchemaGroupRef)item);
					
			if (item is XmlSchemaElement)
			{
				XmlSchemaElement elem=(XmlSchemaElement) item;
				XmlSchemaElement refElem;
				if (!elem.RefName.IsEmpty) refElem=GetRefElement (elem);
				else refElem=elem;

				int num=(elem.MaxOccurs == 1 && !multiValue) ? 1 : 2;
				for (int n=0; n<num; n++)
				{
					if (BindingUse == SoapBindingUse.Literal)
						Add_Element (complexNode, ns, refElem);
					else
						Add_RefType (complexNode, ns, refElem);
				}
			}
			else if (item is XmlSchemaAny)
			{
				TreeNode node=new TreeNode( ("xml")) ;
				complexNode.Nodes.Add(node) ;					
			}
			else if (item is XmlSchemaParticle) 
			{
				Parse_ParticleContent (complexNode, ns, (XmlSchemaParticle)item, multiValue);
			}
		}
		
		void Parse_ChoiceContent (TreeNode inner, string ns, XmlSchemaChoice choice, bool multiValue)
		{
			foreach (XmlSchemaObject item in choice.Items)
				Add_Item (inner, ns, item, multiValue);
		}

		void Parse_SimpleContent (TreeNode inner, XmlSchemaSimpleContent content)
		{
			XmlSchemaSimpleContentExtension ext=content.Content as XmlSchemaSimpleContentExtension;
			XmlSchemaSimpleContentRestriction rst=content.Content as XmlSchemaSimpleContentRestriction;

			if (ext != null)
				Add_Attributes (inner, ext.Attributes, ext.AnyAttribute);
			
			XmlQualifiedName qname=GetContentBaseType (content.Content);
			TreeNode node=new TreeNode( (GetBuiltInTypeName (qname))) ;
			inner.Nodes.Add(node) ;				
		}

		string GetBuiltInTypeName (XmlQualifiedName qname)
		{
			if (qname.IsEmpty)
			{
				return "";
			}

			if (qname.Namespace == XmlSchema.Namespace)
				return qname.Name;

			XmlSchemaComplexType ct=GetComplexTypeByName (qname);
			if (ct != null)
			{
				XmlSchemaSimpleContent sc=ct.ContentModel as XmlSchemaSimpleContent;
				if (sc == null) throw new Exception ("Invalid schema");
				return GetBuiltInTypeName (GetContentBaseType (sc.Content));
			}
			
			XmlSchemaSimpleType st=(XmlSchemaSimpleType) _schemas.Find (qname, typeof(XmlSchemaSimpleType));
			if (st != null)
				return GetBuiltInType (st);

			throw new Exception ("Definition of type " + qname + " not found");
		}

		string GetBuiltInType (XmlSchemaSimpleType st)
		{
			if (st == null) return string.Empty ;

			if (st.Content is XmlSchemaSimpleTypeRestriction) 
			{
				return GetBuiltInTypeName (GetContentBaseType (st.Content));
			}
			else if (st.Content is XmlSchemaSimpleTypeList) 
			{
				string s=GetBuiltInTypeName (GetContentBaseType (st.Content));
				return s + " " + s + " ...";
			}
			else if (st.Content is XmlSchemaSimpleTypeUnion)
			{
				
				XmlSchemaSimpleTypeUnion uni=(XmlSchemaSimpleTypeUnion) st.Content;
				string utype=null;


				if (uni.BaseTypes.Count != 0 && uni.MemberTypes.Length != 0)
					return "string";

				foreach (XmlQualifiedName mt in uni.MemberTypes)
				{
					string qn=GetBuiltInTypeName (mt);
					if (utype != null && qn != utype) return "string";
					else utype=qn;
				}
				return utype;
			}
			else
				return "string";
		}
		

		XmlQualifiedName GetContentBaseType (XmlSchemaObject obj)
		{
			if (obj is XmlSchemaSimpleContentExtension)
				return ((XmlSchemaSimpleContentExtension)obj).BaseTypeName;
			else if (obj is XmlSchemaSimpleContentRestriction)
				return ((XmlSchemaSimpleContentRestriction)obj).BaseTypeName;
			else if (obj is XmlSchemaSimpleTypeRestriction)
				return ((XmlSchemaSimpleTypeRestriction)obj).BaseTypeName;
			else if (obj is XmlSchemaSimpleTypeList)
				return ((XmlSchemaSimpleTypeList)obj).ItemTypeName;
			else
				return null;
		}
		
		const string WsdlNamespace="http://schemas.xmlsoap.org/wsdl/";
		const string SoapEncodingNamespace="http://schemas.xmlsoap.org/soap/encoding/";

		void Parse_ComplexContent (TreeNode inner, string ns, XmlSchemaComplexContent content)
		{
			XmlQualifiedName qname;

			XmlSchemaComplexContentExtension ext=content.Content as XmlSchemaComplexContentExtension;
			if (ext != null) qname=ext.BaseTypeName;
			else 
			{
				XmlSchemaComplexContentRestriction rest=(XmlSchemaComplexContentRestriction)content.Content;
				qname=rest.BaseTypeName;
				if (qname == arrayType) 
				{
					Parse_ArrayType (rest, out qname);
					XmlSchemaElement elem=new XmlSchemaElement ();
					elem.Name="Item";
					elem.SchemaTypeName=qname;
					
					TreeNode node=new TreeNode("arrayType" + SoapEncodingNamespace + qname.Name + "[2]") ;
					inner.Nodes.Add(node) ;
					Add_Item (inner, ns, elem, true);
					return;
				}
			}
			
			

			// Add base map members to this map
			XmlSchemaComplexType ctype=GetComplexTypeByName (qname);

			Add_ComplexAttributes (inner, ctype);
			//Add base content first
			Add_ComplexElements (inner, ns, ctype);
			
			if (ext != null) 
			{
				// Add the members of this map
				Add_Attributes (inner, ext.Attributes, ext.AnyAttribute);
				if (ext.Particle != null)
					Parse_ParticleComplexContent (inner, ns, ext.Particle);
			}
			
			
		}
		
		void Parse_ArrayType (XmlSchemaComplexContentRestriction rest, out XmlQualifiedName qtype)
		{
			XmlSchemaAttribute arrayTypeAt=GetArrayAttribute (rest.Attributes);
			XmlAttribute[] uatts=arrayTypeAt.UnhandledAttributes;
			if (uatts == null || uatts.Length == 0) throw new Exception ("arrayType attribute not specified in array declaration");
			
			XmlAttribute xat=null;
			foreach (XmlAttribute at in uatts)
				if (at.LocalName == "arrayType" && at.NamespaceURI == WsdlNamespace)
				{ xat=at; break; }
			
			if (xat == null) 
				throw new Exception ("arrayType attribute not specified in array declaration");
			
			string arrayType=xat.Value;
			string type, ns;
			int i=arrayType.LastIndexOf (":");
			if (i == -1) ns=string.Empty;
			else ns=arrayType.Substring (0,i);
			
			int j=arrayType.IndexOf ("[", i+1);
			if (j == -1) throw new Exception ("Cannot parse WSDL array type: " + arrayType);
			type=arrayType.Substring (i+1);
			type=type.Substring (0, type.Length-2);
			
			qtype=new XmlQualifiedName (type, ns);
		}
		
		XmlSchemaAttribute GetArrayAttribute (XmlSchemaObjectCollection atts)
		{
			foreach (object ob in atts)
			{
				XmlSchemaAttribute att=ob as XmlSchemaAttribute;
				if (att != null && att.RefName == arrayTypeRefName) return att;
				
				XmlSchemaAttributeGroupRef gref=ob as XmlSchemaAttributeGroupRef;
				if (gref != null)
				{
					XmlSchemaAttributeGroup grp=(XmlSchemaAttributeGroup) _schemas.Find (gref.RefName, typeof(XmlSchemaAttributeGroup));
					att=GetArrayAttribute (grp.Attributes);
					if (att != null) return att;
				}
			}
			return null;
		
		}

		private const string SOMListName="System.Xml.Schema.XmlSchemaObjectCollection";

		void Parse_AnyObject(TreeNode inner, XmlSchemaObject obj)
		{
			Type type=obj.GetType();
			PropertyInfo[] properties=type.GetProperties ();			
				
			foreach (PropertyInfo property in properties)
			{
				if (property.PropertyType.FullName.Equals(SOMListName))
				{
					XmlSchemaCollection somList=property.GetValue(obj, null) as XmlSchemaCollection;

					if (somList != null)
						foreach (XmlSchemaObject somObj in somList) 
						{
							Parse_AnyObject (inner, somObj);
						}
				}				
			}	

		}
		void Parse_SimpleType (TreeNode inner, XmlSchemaSimpleType stype)
		{
			if (inner.Text == string.Empty)
				inner.Text= (GetBuiltInType (stype));

			if (inner.Tag == null)
			{
				NodeData data=new NodeData(stype, stype.QualifiedName, "") ;
				inner.Tag=data;
			}
			
		}
		
		XmlSchemaParticle GetRefGroupParticle (XmlSchemaGroupRef refGroup)
		{
			XmlSchemaGroup grp=(XmlSchemaGroup) _schemas.Find (refGroup.RefName, typeof (XmlSchemaGroup));
			return grp.Particle;
		}

		XmlSchemaElement GetRefElement (XmlSchemaElement elem)
		{
			if (elem.RefName.Namespace == XmlSchema.Namespace)
			{
				if (anyElement != null) return anyElement;
				return anyElement;
			}
			return (XmlSchemaElement) _schemas.Find (elem.RefName, typeof(XmlSchemaElement));
		}
		

		
		XmlSchemaAttribute GetRefAttribute (XmlQualifiedName refName)
		{
			if (refName.Namespace == XmlSchema.Namespace)
			{
				XmlSchemaAttribute at=new XmlSchemaAttribute ();
				at.Name=refName.Name;
				at.SchemaTypeName=new XmlQualifiedName ("string",XmlSchema.Namespace);
				return at;
			}
			return (XmlSchemaAttribute) _schemas.Find (refName, typeof(XmlSchemaAttribute));
		}
		
		void Add_RefType (TreeNode inner, string ns, XmlSchemaElement elem)
		{
			if (elem.SchemaTypeName.Namespace == XmlSchema.Namespace || _schemas.Find (elem.SchemaTypeName, typeof(XmlSchemaSimpleType)) != null)
				Add_Element (inner, ns, elem);
			else
			{
				TreeNode node=new TreeNode(elem.Name + ns) ;
				TreeNode node2=new TreeNode("href" + "#id" + (_queue.Count+1)) ;
				node.Nodes.Add(node2) ;
				inner.Nodes.Add(node) ;				
				_queue.Add (new Encoded (ns, elem));
			}
		}
		
		void Parse_QueuedType (TreeNode inner)
		{
			for (int n=0; n<_queue.Count; n++)
			{
				Encoded ec=(Encoded) _queue[n];
				XmlSchemaComplexType st=GetComplexTypeByName (ec.Element.SchemaTypeName);
				Parse_ComplexType (inner, st, ec.Element.SchemaTypeName, n+1);
			}
		}
		
		XmlSchemaComplexType GetComplexTypeByName (XmlQualifiedName qname)
		{
			if (qname.Name.IndexOf ("[]") != -1)
			{
				XmlSchemaComplexType stype=new XmlSchemaComplexType ();
				stype.ContentModel=new XmlSchemaComplexContent ();
				
				XmlSchemaComplexContentRestriction res=new XmlSchemaComplexContentRestriction ();
				stype.ContentModel.Content=res;
				res.BaseTypeName=arrayType;
				
				XmlSchemaAttribute att=new XmlSchemaAttribute ();
				att.RefName=arrayTypeRefName;
				res.Attributes.Add (att);
									
					
				return stype;
			}
				
			return (XmlSchemaComplexType) _schemas.Find (qname, typeof(XmlSchemaComplexType));
		}
		
		string GetQualifiedNameString (TreeNode inner, XmlQualifiedName qname)
		{
			TreeNode node=new TreeNode("xmlns q1=" + qname.Namespace);
			return "q1:" + qname.Name;
		}
				
	}
	
	
}
