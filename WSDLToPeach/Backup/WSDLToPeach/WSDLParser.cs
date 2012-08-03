using System;
using System.Web.Services.Description;
using System.Web.Services;
using System.Collections.Generic;
using System.Collections;
using System.Xml.Schema;
using System.Xml;
using System.Xml.Serialization;
using System.Linq;
using System.Text;
using System.Reflection;

namespace WSDLToPeach
{
    class WSDLParser
    {
        private ServiceDescriptionCollection serviceDescriptionCollection;
        private ServiceDescription serviceDescription;
        private Hashtable schemas;
        private PeachEmitter emitter;
        private ArrayList schemaAttrList;
        private string wsdlFileName;
        private string peachFileName;

        public void CreatePeachFile()
        {
        }

        public WSDLParser(string wsdlFileName, string peachFileName)
        {

            this.wsdlFileName = wsdlFileName;
            this.peachFileName = peachFileName;
        }
        public void go()
        {
            go(""); 
        }

        public void go(String stage) {
            try
            {
                emitter = new PeachEmitter(peachFileName);
                Console.WriteLine("Importing " + wsdlFileName);
                serviceDescription = ServiceDescription.Read(wsdlFileName);

                if (serviceDescription == null)
                {
                    return;
                }

                schemas = new Hashtable();
                schemaAttrList = new ArrayList();
                FindImports(serviceDescription);

                Console.WriteLine("schema:\n");

                foreach (DictionaryEntry dictEntry in schemas)
                {
                    XmlSchema schema = (XmlSchema)dictEntry.Value;
                    serviceDescription.Types.Schemas.Add(schema);
                    Console.WriteLine(schema.SourceUri);
                }

                serviceDescriptionCollection = new ServiceDescriptionCollection();
                serviceDescriptionCollection.Add(serviceDescription);

                ServiceDescription serviceRef = serviceDescription;

                switch (stage)
                {
                    case "b":
                        break;
 
                    case "t":
                        ProcessTypeCollection(serviceRef);
                        break; 

                    case "p":
                        ProcessPortTypeCollection(serviceRef);
                        break; 

                    case "m":
                        ProcessMessageCollection(serviceRef);
                        break;

                    case "s":
                        ProcessServiceCollection(serviceDescriptionCollection);
                        break; 

                    default:
                        ProcessTypeCollection(serviceRef);
                        ProcessPortTypeCollection(serviceRef);
                        ProcessMessageCollection(serviceRef);
                        ProcessServiceCollection(serviceDescriptionCollection);
                    break; 
                }

                emitter.Output();

            } catch (Exception e) {
                Console.WriteLine(e.ToString());
            }
        }

        public void AddGlobalAttribute(XmlSchemaAttribute attr)
        {
            if (attr != null)
                schemaAttrList.Add(attr);
        }

        public void FindImports(ServiceDescription serviceDescription)
        {
            foreach (Import import in (ImportCollection)serviceDescription.Imports)
            {
                XmlSchema importedSchema = LoadImport(import.Location);
                if (importedSchema != null)
                {
                    AddSchema(importedSchema);
                }
            }

            foreach (XmlSchema schema  in serviceDescription.Types.Schemas) 
            {
                foreach (XmlSchemaImport import in schema.Includes)
                {

                    if(import != null)
                        if (import.SchemaLocation != null)
                        {

                            Console.WriteLine(import.SchemaLocation);

                            XmlSchema importedSchema = LoadImport(import.SchemaLocation);

                            if (importedSchema != null)
                            {
                                AddSchema(importedSchema);
                                FindImports(importedSchema);
                            }
                        }
                }
            }
        }

        private void AddSchema(XmlSchema schema) {
            if (!schemas.ContainsKey(schema.SourceUri))
                schemas.Add(schema.SourceUri, schema);
        }

        public void FindImports(XmlSchema schema)
        {
           foreach (XmlSchemaImport import in schema.Includes)
           {
               XmlSchema importedSchema = LoadImport(import.SchemaLocation);

               if (importedSchema != null)
               {
                        AddSchema(importedSchema);
                        FindImports(importedSchema);
               }
           }
        }

        private XmlSchema LoadImport(string import)
        {
            Console.WriteLine("Loading Import {0}", import);
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.ProhibitDtd = false;
            XmlReader reader = XmlReader.Create(import,settings);
            XmlSchema schema = XmlSchema.Read(reader, null);

            return schema;
        }

        public void ProcessImportCollection(ServiceDescription serviceDescription)
        {
            if (serviceDescription.Imports != null)
            {
                DisplayImports(serviceDescription.Imports, "");
            }
        }

        public void ProcessTypeCollection(ServiceDescription serviceDescription)
        {
            if (serviceDescription.Types != null)
                if (serviceDescription.Types.Schemas != null)
                    ParseTypes(serviceDescription.Types, "");
        }

        public void ProcessPortTypeCollection(ServiceDescription serviceDescription)
        {
            if (serviceDescription.PortTypes != null)
                ParsePortTypes(serviceDescription.PortTypes, "");

        }

        public void ProcessServiceCollection(ServiceDescriptionCollection serviceDescriptionCollection)
        {
            if (serviceDescriptionCollection != null)
                ParseServices(serviceDescriptionCollection, "");
        }

        public void ProcessMessageCollection(ServiceDescription serviceDescription)
        {
            if (serviceDescription.Messages != null)
                ParseMessages(serviceDescription.Messages, "");
        }

        private void DisplayImports(Object obj, string indent)
        {
            Console.WriteLine("{0}{1}", indent, obj);

            if (obj is System.Web.Services.Description.ImportCollection)
            {
                foreach (Import import in (ImportCollection)obj)
                {

                    Console.WriteLine(indent + "Namespace: " + import.Namespace);
                    Console.WriteLine(indent + "Location: " + import.Location);
                }
            }
        }

        private void ParseMessages(Object obj, string indent)
        {
            Console.WriteLine("{0}{1}", indent, obj);

            if (obj is System.Web.Services.Description.MessageCollection)
            {
                foreach (Message message in (MessageCollection)obj)
                {

                    Console.WriteLine(indent + message.Name);
                    ArrayList parts = new ArrayList();
                    foreach (Object part in message.Parts)
                    {
                        if (part is System.Web.Services.Description.MessagePart)
                        {
                            MessagePart messagePart = (MessagePart)part;
                            Console.WriteLine(indent + "Name:" + messagePart.Name);
                            Console.WriteLine(indent + "Element:" + messagePart.Element);
                            if (messagePart.Element == null || messagePart.Element.ToString() == "")
                            {
                                parts.Add(new string[] { messagePart.Name, messagePart.Type.ToString() });
                            }
                            else
                            {
                                parts.Add(new string[] { messagePart.Name, GetName(messagePart.Element) });
                            }

                        }
                    }
                    AddMessage(message.Name, parts);
                }
            }
       }

        private void ParsePortTypes(Object obj, string indent)
        {
            Console.WriteLine("{0}{1}", indent, obj);

            if (obj is PortTypeCollection)
            {
                foreach (PortType portType in (PortTypeCollection)obj)
                {
                    Console.WriteLine(indent + portType.Name);

                    foreach (Operation operation in portType.Operations)
                    {

                        ParseOperation(operation, indent + "\t");
                    }

                    Console.WriteLine("***TODO: PortType Extensions support***");

                    //foreach (Object extension in portType.Extensions)
                     //DisplayPortTypes(extension, indent + "\t");

                }
                return;
            }

   
        }

        private void ParseOperation(Operation operation, String indent)
        {
            ParseOperation(operation, "", "", indent);
        }

        private void ParseOperation(Operation operation, String transport, String address, String indent) {
           
            Console.WriteLine(indent + operation.Name);

           
            if(operation.Messages != null)
            {
                if (operation.Messages.Input != null)
                    if(operation.Messages.Input.Message != null)
                        emitter.AddOperation(operation.Name + "Input", transport, address, operation.Messages.Input.Message.Name, "input");
            }

            if(operation.Messages != null)
            {
               if(operation.Messages.Output!= null)
                if (operation.Messages.Output != null)
                    emitter.AddOperation(operation.Name + "Output" , transport, address, operation.Messages.Output.Message.Name, "output");
            }

                /*foreach (Object extension in operation.Extensions)
                {
                    DisplayPortTypes(extension, indent + "\t");
                }*/
        }

        private void ParseServices(ServiceDescriptionCollection serviceDescriptionCollection, string indent)
        {

            ServiceCollection services = serviceDescriptionCollection[0].Services;
            String transportType = "";
                if(services != null) 
                foreach (Service service in services)
                {
                        if (service.Ports != null)
                            foreach (Port port in service.Ports)
                            {
                                if (port is Port)
                                {
                                    Console.WriteLine(indent + "Name: {0}, Binding:{1}", port.Name, port.Binding);
                                    XmlQualifiedName bindingName = port.Binding;
                                    Binding binding = serviceDescriptionCollection.GetBinding(bindingName);
                                    PortType portType = serviceDescriptionCollection.GetPortType(binding.Type);
                                    

                                    foreach (OperationBinding operationBinding in binding.Operations)
                                    {

                                        Console.WriteLine(operationBinding.ToString());
                                        foreach ( Operation operation in portType.Operations)
                                        {
                                            if (operationBinding.Name.Equals(operation.Name))
                                            {

                                                    SoapOperationBinding soapBinding =  (SoapOperationBinding) operationBinding.Extensions.Find(typeof(SoapOperationBinding));
                                                    if(soapBinding != null) { 
                                                             ParseOperation(operation, "Soap", soapBinding.SoapAction  , indent);
                                                    }

                                                     Soap12OperationBinding soap12Binding =  (Soap12OperationBinding) operationBinding.Extensions.Find(typeof(Soap12OperationBinding));
                                                    if(soap12Binding != null) { 
                                                            ParseOperation(operation, "Soap12", soap12Binding.SoapAction, indent); 
                                                    }

                                                    HttpBinding httpBinding =  (HttpBinding) operationBinding.Extensions.Find(typeof(HttpBinding));
                                                    if(httpBinding != null)
                                                    foreach (HttpOperationBinding hob in operationBinding.Extensions)
                                                    {
                                                        ParseOperation(operation, transportType, service.Name, indent);
                                                    }
                                            }
                                        }
                                    }
                                }
                            }
                }
        } //END ParseServices 

        private void ParseSimpleTypes(Object obj, string indent, ArrayList elements)
        {

            if (elements == null)
                elements = new ArrayList();

            if (obj is XmlSchemaSimpleType)
            {
                XmlSchemaSimpleType simpleType = (XmlSchemaSimpleType)obj;
                AddSimpleType(simpleType);
                return;
            }
        } //End ParseSimpleType 

        public string GetName(XmlQualifiedName qName)
        {
            if (qName == null)
                return "";

            string name = "";
            string[] splitUrl = qName.Namespace.Split('/');

            if (splitUrl != null)
            {
                if (splitUrl.Last() == "XMLSchema")
                {
                    return qName.Name;
                }
                name += splitUrl.Last() + "_";
            }
            else
            {
                name += qName.Namespace;
            }

            name += qName.Name;
            name = name.Replace(':', '_');
            return name;
        }

        private void ParseTypes(Object obj, string indent)
        {
            Console.WriteLine("{0}{1}", indent, obj);

            if (obj is System.Web.Services.Description.Types)
            {
                Types types = (Types)obj;

                foreach (Object schemaItem in types.Schemas)
                {
                    if (schemaItem is System.Xml.Schema.XmlSchema)
                    {
                        XmlSchema schema = (XmlSchema) schemaItem;

                        foreach (Object type in schema.SchemaTypes.Values)
                        {
                            if (type is XmlSchemaComplexType)
                            {
                                XmlSchemaComplexType  complexType = (XmlSchemaComplexType) type; 
                                XmlNode x = emitter.AddElement(GetName(complexType.QualifiedName), complexType.Name);
                                AddComplexType(x, (XmlSchemaComplexType) type, schemaAttrList);
                            }

                            if (type is XmlSchemaSimpleType)
                            {
                                XmlSchemaSimpleType simpleType = (XmlSchemaSimpleType)type;
                                XmlNode x = emitter.AddElement(GetName(simpleType.QualifiedName), simpleType.Name); 
                                ParseSimpleTypes(simpleType, indent, null);
                            }
                        }

                        foreach (DictionaryEntry elementEntry in schema.Elements)
                        {
                            Object element = elementEntry.Value;

                            //NEEDS FIXN
                            if (element is System.Xml.Schema.XmlSchemaElement)
                            {
                                XmlSchemaElement schemaElement = (XmlSchemaElement)element;
                                Console.WriteLine(indent + "Name: {0}", schemaElement.Name);
                                Console.WriteLine(indent + "Type: {0}", schemaElement.SchemaTypeName);
                                Console.WriteLine(indent + "QName: {0}", schemaElement.SchemaType);

                                Console.WriteLine(indent + "Ref:  {0}", schemaElement.RefName);
                                Console.WriteLine();

                                //ParseTypes(schemaElement, indent + "\t");
                                string schemaName = GetName(schemaElement.SchemaTypeName);
                                string qName = GetName(schemaElement.QualifiedName);
                                XmlNode x = emitter.AddElement(qName, schemaName);

                                if (schemaElement.RefName.IsEmpty)
                                {
                                    if (schemaElement.SchemaType is XmlSchemaSimpleType)
                                    {
                                     ParseSimpleTypes(schemaElement.SchemaType, indent, null);
                                    }

                                    if (schemaElement.SchemaType is XmlSchemaComplexType)
                                    {
                                    XmlSchemaComplexType complex = (XmlSchemaComplexType)schemaElement.SchemaType;
                                    AddComplexType(x, complex, schemaAttrList);
                                    }
                                }
                            }

                            if (element is System.Xml.Schema.XmlSchemaObjectCollection)
                            {
                                XmlSchemaObjectCollection objectCollection = (XmlSchemaObjectCollection)element;
                                foreach (XmlSchemaObject item in objectCollection)
                                {
                                    Console.Out.WriteLine("TODO Lazy Programmer!");
                                    //DisplayTypes(element, indent + "\t");
                                }
                            }

                            if (element is XmlSchemaAny)
                            {
                                XmlSchemaAny any = (XmlSchemaAny)element;
                                Console.WriteLine(indent + "Any Type");
                            }
                        }
                    }
                }
            }
        }


        public void AddComplexType(XmlSchemaComplexType schemaComplexType, ArrayList globalAttrList)
        {
            AddComplexType(null, schemaComplexType, globalAttrList);

        }

        public void AddComplexType(XmlNode node, XmlSchemaComplexType schemaComplexType, ArrayList globalAttrList)
        {
            XmlNode tmpNode = null;
            if (node == null)
                node = emitter.peachNode;

            if (schemaComplexType == null)
                return;

            if (node == emitter.peachNode)
                tmpNode = emitter.AddDataModel(node, GetName(schemaComplexType.QualifiedName), null);
            else
                tmpNode = node;

            if (tmpNode == null)
            {
                Console.WriteLine("tmpNode is somehow null");
            }

            if (schemaComplexType.AnyAttribute != null)
            {
                //AddCustomType(tmpNode, "anyAttr", "CustomAttribute");
            }

            if (schemaComplexType.Particle != null)
            {

                if (schemaComplexType.Particle is System.Xml.Schema.XmlSchemaSequence)
                {
                    XmlSchemaSequence sequence = (XmlSchemaSequence)schemaComplexType.Particle;
                    //XmlNode tmpNodeChild = emitter.AddBlock(tmpNode, "Sequence", null);

                    foreach (XmlSchemaObject schemaObj in sequence.Items)
                    {
                        AddWsdlType(tmpNode, schemaObj);
                    }
                }

                if (schemaComplexType.Particle is System.Xml.Schema.XmlSchemaChoice)
                {
                    XmlSchemaChoice sequence = (XmlSchemaChoice)schemaComplexType.Particle;
                    XmlNode tmpNodeChild = emitter.AddChoice(tmpNode, "Choice", null);
                    foreach (XmlSchemaObject schemaObj in sequence.Items)
                    {
                        AddWsdlType(tmpNodeChild, schemaObj);
                    }
                }

                if (schemaComplexType.Particle is System.Xml.Schema.XmlSchemaAll)
                {
                    XmlSchemaAll schemaAll = (XmlSchemaAll)schemaComplexType.Particle;
                    //XmlNode tmpNodeChild = emitter.AddBlock(tmpNode, "All", null);
                    foreach (Object schemaObj in schemaAll.Items)
                    {
                        AddWsdlType(tmpNode, schemaObj);
                    }
                }
            }

            if (schemaComplexType.ContentModel != null)
                if (schemaComplexType.ContentModel.Content != null)
                    AddWsdlType(tmpNode, schemaComplexType.ContentModel.Content);

            if (schemaComplexType.Attributes != null)
            {
                foreach (Object attr in schemaComplexType.Attributes)
                {
                    AddWsdlType(tmpNode, attr);
                }

                foreach (Object attr in globalAttrList)
                {
                    AddWsdlType(tmpNode, attr);
                }
            }
        }

        public void AddSimpleType(XmlSchemaSimpleType schemaSimpleType)
        {
            XmlNode tmpNode = emitter.AddDataModel(emitter.peachNode, GetName(schemaSimpleType.QualifiedName), null);

            Object obj = schemaSimpleType.Content;

            if (obj is System.Xml.Schema.XmlSchemaSimpleTypeRestriction)
            {
                XmlSchemaSimpleTypeRestriction restriction = (XmlSchemaSimpleTypeRestriction)obj;
                //Console.WriteLine(indent + "Name:" + restriction.BaseTypeName);
                //XmlNode baseNode = AddType(tmpNode, restriction.BaseTypeName.Name, restriction.BaseTypeName.Name);
                Console.WriteLine(restriction.BaseTypeName);

                XmlNode choiceNode = emitter.AddChoice(tmpNode, restriction.BaseTypeName.Name + "Choice");

                foreach (XmlSchemaFacet facet in restriction.Facets)
                {
                    AddType(choiceNode, facet.Value, restriction.BaseTypeName.Name, facet.Value, true);
                }
            }

            if (obj is XmlSchemaSimpleTypeContent)
            {
                XmlSchemaSimpleTypeContent simpleTypeContent = (XmlSchemaSimpleTypeContent)obj;
                //TODO: code this
            }

            if (obj is XmlSchemaSimpleContent)
            {
                XmlSchemaSimpleContent simpleContent = (XmlSchemaSimpleContent)obj;
                //DisplaySimpleTypes(simpleContent.Content, indent + "\t", elements);
            }



            if (obj is XmlSchemaSimpleTypeList)
            {
                XmlSchemaSimpleTypeList list = (XmlSchemaSimpleTypeList)obj;
                //XmlNode tmpChild = emitter.AddBlock(tmpNode, "list", null);
                if (list.ItemType != null)
                {
                    AddType(tmpNode, GetName(list.ItemType.QualifiedName), null);
                }

                AddType(tmpNode, "list", GetName(list.ItemTypeName));

            }

            if (obj is XmlSchemaSimpleTypeUnion)
            {
                XmlSchemaSimpleTypeUnion union = (XmlSchemaSimpleTypeUnion)obj;
                tmpNode = emitter.AddChoice(tmpNode, "Union", null);
                //Console.WriteLine(indent + union.SourceUri);
                if (union.BaseMemberTypes != null)
                {
                    foreach (Object type in union.BaseMemberTypes)
                    {
                        AddType(tmpNode, "", "");
                    }
                }

                if (union.MemberTypes != null)
                {
                    foreach (XmlQualifiedName qName in union.MemberTypes)
                    {
                        AddType(tmpNode, GetName(qName), GetName(qName));
                    }
                }
            }
        }

        // TODO:  asstastic make a class to pass around type state.
        private XmlNode AddType(XmlNode node, string name, string type, string value, bool enumeration)
        {
         return AddType(node, name, type, value, enumeration, "1", "1");
        }

        private XmlNode AddType(XmlNode node, string name, string type, bool enumeration)
        {
            return AddType(node, name, type, "", enumeration, "1", "1");
        }

        private XmlNode AddType(XmlNode node, string name, string type)
        {
            return AddType(node, name, type, "", false, "1", "1");
        }

        private XmlNode AddType(XmlNode node, string name, string type, string min, string max)
        {
            return AddType(node, name, type, "", false, min, max);
        }

        private XmlNode AddType(XmlNode node, string name, string type, string value, bool enumeration, string min, string max )
        {
            XmlNode returnNode = null;

            if (type == null)
                return null;

            Boolean baseType = false; 

            //ass formatting of namespaces
            string  data = ""; 
            string origionalType = type.Replace('.', '_');
            string[] types = type.Split(':');
            string[] names = name.Split('_');
            name = name.Replace('.', '_').Replace(':', '_');
            string nodeName = names.Last().Replace('.', '_');

            if (types == null)
                type = type.ToLower();
            else
                type = types.Last().ToLower();

            if (type == "base64binary")
            {
                returnNode = emitter.AddXMLElement(node, nodeName);
                emitter.AddBlob(returnNode, name, "hex");
                return returnNode;
            }

            switch (type)
            {
                case "qname":
                    data = "";
                    baseType = true; 
                    break;

                case "anyuri":
                    data = "default";
                    baseType = true; 
                    break; 

                case "unsignedint":
                    data = "32";
                    baseType = true; 
                    break; 

                case "boolean":
                    data = "false";
                    baseType = true; 
                    break; 

                case "int":
                    data = "41";
                    baseType = true; 
                    break; 

                case "string":
                    data = "default";
                    baseType = true; 
                    break; 

                case "double":
                    data = "0.0";
                    baseType = true; 
                    break;

            }

            if (baseType)
            {
                returnNode = node; 

                if(!enumeration)
                    returnNode = emitter.AddXMLElement(node, nodeName);

                if (value == "")
                    emitter.AddString(returnNode, name + "Data", data, min, max);
                else
                    emitter.AddString(returnNode, name + "Data", value , min, max); 

                return returnNode;
            }

            return emitter.AddXMLElement(node, name, origionalType.Replace(':', '_').Replace('.', '_'));
        }

        public void AddMessage(string name, ArrayList parts)
        {
            XmlNode tmpNode = emitter.AddDataModel(emitter.peachNode, name, null);
            foreach (string[] refname in parts)
            {
                AddType(tmpNode, refname[0], refname[1]);
            }
        }

        private void AddWsdlType(XmlNode node, Object obj)
        {

            if (obj is XmlSchemaElement)
            {
                XmlSchemaElement schemaElement = (XmlSchemaElement)obj;
                if (schemaElement.RefName != null && schemaElement.RefName.ToString() != "")
                {
                    AddType(node, schemaElement.Name, GetName(schemaElement.QualifiedName));
                    return;
                }

                if (schemaElement.SchemaTypeName != null)
                {
                    AddType(node, GetName(schemaElement.QualifiedName), GetName(schemaElement.SchemaTypeName), schemaElement.MinOccurs.ToString(), schemaElement.MinOccurs.ToString());
                }

                return;
            }

            if (obj is XmlSchemaAny)
            {
                XmlSchemaAny any = (XmlSchemaAny)obj;
                AddType(node, "Any", "Any");
                return;
            }

            if (obj is XmlSchemaSimpleContentExtension)
            {
                XmlSchemaSimpleContentExtension ext = (XmlSchemaSimpleContentExtension)obj;
                AddType(node, GetName(ext.BaseTypeName), GetName(ext.BaseTypeName));

                foreach (Object attr in ext.Attributes)
                {
                    AddWsdlType(node, attr);
                }

                if (ext.AnyAttribute != null)
                {
                    //AddCustomAttributeType(node, "any", "any");
                }
                return;
            }

            if (obj is XmlSchemaAttributeGroupRef)
            {
                XmlSchemaAttributeGroupRef attrGroupRef = (XmlSchemaAttributeGroupRef)obj;
                return;
            }

            if (obj is XmlSchemaComplexContentExtension)
            {
                XmlSchemaComplexContentExtension contentExtension = (XmlSchemaComplexContentExtension) obj;

                if (contentExtension.Particle is XmlSchemaSequence)
                {
                    XmlSchemaSequence seq = (XmlSchemaSequence) contentExtension.Particle;
                    foreach (XmlSchemaObject item in seq.Items){ 
                        AddWsdlType(node, item);    
                    }
                }
            }

            if (obj is XmlSchemaAttributeGroup)
            {
                XmlSchemaAttributeGroup attrGroup = (XmlSchemaAttributeGroup)obj;
                foreach (Object attrGroupAttr in attrGroup.Attributes)
                {
                    AddWsdlType(node, (XmlSchemaAttribute)attrGroupAttr);
                }
            }

            if (obj is XmlAnyAttributeAttribute)
            {
                XmlAnyAttributeAttribute attr = (XmlAnyAttributeAttribute)obj;
                //AddCustomAttributeType(node, "AttrANY" , "ATTRANY");
                return;
            }

            if (obj is XmlSchemaAttribute)
            {
                XmlSchemaAttribute attr = (XmlSchemaAttribute)obj;
                //AddCustomAttributeType(node, attr.Name, GetName(attr.SchemaTypeName));
                return;
            }

            if (obj is XmlSchemaComplexContentRestriction)
            {
                XmlSchemaComplexContentRestriction restriction = (XmlSchemaComplexContentRestriction)obj;

                AddType(node, GetName(restriction.BaseTypeName), GetName(restriction.BaseTypeName));

                foreach (XmlSchemaAttribute attr in restriction.Attributes)
                {
                    AddWsdlType(node, attr);
                }

                if (restriction.AnyAttribute != null)
                {
                    //AddCustomAttributeType(node, "any", "any");
                }
                return;
            }
        }
    }
}
