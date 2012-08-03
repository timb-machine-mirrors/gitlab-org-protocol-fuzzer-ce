using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace WSDLToPeach
{
    class PeachEmitter : XMLEmitter
    {

        public XmlNode peachNode;
        public XmlNode runNode;
        public XmlNode dataModelsNode = null; // tmp node to attach Datamodels before sorting.  
        public XmlNode soapHeaderNode;
        public XmlNode soapNode;
        public XmlNode loggerNode;
        private string filename;

        public PeachEmitter(string filename)
        {
            this.filename = filename;
            XmlNode xmlTmpNode; 

            try
            {

                //TODO: Part this out into methods 
                xmlTmpNode = xdoc.CreateXmlDeclaration("1.0", "utf-8", null);
                xdoc.AppendChild(xmlTmpNode);
                peachNode = xdoc.CreateElement("Peach");
                xdoc.AppendChild(peachNode);

                peachNode.Attributes.Append(CreateAttribute("xmlns","http://phed.org/2008/Peach"));
                peachNode.Attributes.Append(CreateAttribute("xmlns:xsi", "http://www.w3.org/2001/XMLSchema-instance"));
                peachNode.Attributes.Append(CreateAttribute("xsi:schemaLocation", "http://phed.org/2008/Peach ../../../../peach/peach.xsd"));
                peachNode.Attributes.Append(CreateAttribute("version", "1.0"));
                peachNode.Attributes.Append(CreateAttribute("author", "Emitter"));
                peachNode.Attributes.Append(CreateAttribute("description", "EmittedPeachWSDL"));

                xmlTmpNode = xdoc.CreateElement("Include");
                xmlTmpNode.Attributes.Append(CreateAttribute("ns", "default"));
                xmlTmpNode.Attributes.Append(CreateAttribute("src", "file:defaults.xml"));
                peachNode.AppendChild(xmlTmpNode);

                xmlTmpNode = xdoc.CreateElement("Include");
                xmlTmpNode.Attributes.Append(CreateAttribute("ns", "pt"));
                xmlTmpNode.Attributes.Append(CreateAttribute("src", "file:PeachTypes.xml"));
                peachNode.AppendChild(xmlTmpNode);

                AddDataModel(peachNode, "EmptyBlock");

                soapNode = AddDataModel(peachNode, "SoapEnvelope");
                xmlTmpNode = AddXMLElement(soapNode, "Envelope", " http://schemas.xmlsoap.org/soap/envelope","",null);

                AddXMLAttribute(xmlTmpNode, "xmlns:soap", "soap", "http://www.w3.org/2000/xmlns/","http://schemas.xmlsoap.org/soap/envelope");
                AddXMLAttribute(xmlTmpNode, "xmlns:xsi", "xsi", "http://www.w3.org/2000/xmlns/", "http://www.w3.org/2001/XMLSchema-instance");
                AddXMLAttribute(xmlTmpNode, "xmlns:xsd", "xsd","http://www.w3.org/2000/xmlns", "http://www.w3.org/2001/XMLSchema");
                
                soapHeaderNode = AddXMLElement(soapNode, "Header", "http://schemas.xmlsoap.org/soap/envelope", "", null);
                xmlTmpNode = AddXMLElement(soapNode, "Body", "http://schemas.xmlsoap.org/soap/envelope", "", null);

                runNode = xdoc.CreateElement("Run"); 
                runNode.Attributes.Append(CreateAttribute("name", "DefaultRun"));

                loggerNode = xdoc.CreateElement("Logger");
                loggerNode.Attributes.Append(CreateAttribute("class", "logger.Filesystem"));
                runNode.AppendChild(loggerNode);

                xmlTmpNode = xdoc.CreateElement("Param");
                xmlTmpNode.Attributes.Append(CreateAttribute("name", "path"));
                xmlTmpNode.Attributes.Append(CreateAttribute("value", "C:\\peach\\logtest"));
                loggerNode.AppendChild(xmlTmpNode);

                xmlTmpNode = xdoc.CreateElement("DataModel");
                xmlTmpNode.Attributes.Append(CreateAttribute("name", "Any"));
                peachNode.AppendChild(xmlTmpNode);

                AddOperation("SoapTest","stdout" ,"", "SoapEnvelope", ""); 
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
                Console.WriteLine(e.Message);
            }
        }

        public void AddOperation(string name, string transport, string address, string message, string type)
        {

            AddRef( runNode, "Test", name + "Test" + transport );

            name = name.Replace('.', '_');
            name = name.Replace(':', '_');

            XmlNode tmpNode = AddTest( peachNode, name + "Test" + transport );
            AddRef( tmpNode, "StateModel", name + "StateModel" );
            
            switch ( transport.ToLower() ) {
                case "stdout":
                    tmpNode = AddPublisher(tmpNode, name +"Publisher", "stdout.Stdout"); 
                    break;
                case "soap":
                case "soap12": 
                    if (type == "input")
                    {
                        tmpNode = AddPublisher(tmpNode, name + "Publisher" + transport, "http.HttpPost");
                        AddParam(tmpNode, "url", address);
                        /*
                        tmpNode = AddPublisher(tmpNode, name + "Publisher" + transport, "tcp.Tcp");
                        AddParam(tmpNode, "host", address);
                        AddParam(tmpNode, "port", "80");
                         */
                    }

                    if (type == "output")
                    {
                        tmpNode = AddPublisher(tmpNode, name + "Publisher" + transport, "tcp.TcpListener");
                        AddParam(tmpNode, "host", "127.0.0.1");
                        AddParam(tmpNode, "port", "80");
                    }

                        XmlNode soapDataNode = AddDataModel(peachNode, message + transport, "SoapEnvelope");
                        tmpNode = AddXMLElement(soapDataNode, "soapBodyData", message);


                        tmpNode = AddStateModel(name + "StateModel", "initial");
                        tmpNode = AddState(tmpNode, "initial");
                        tmpNode = AddAction(tmpNode, name + "Action", "output");
                        AddRef(tmpNode, "DataModel", message + transport);
                    break;

                case "http":
                    tmpNode = AddPublisher(tmpNode, name+"Publisher", "stdout.Stdout");
                    break;
                default:
                    tmpNode = AddPublisher(tmpNode, name+"Publisher", "stdout.Stdout");
                    break;
            }

            if (transport.ToLower() != "soap")
            {
                tmpNode = AddStateModel(name + "StateModel", "initial");
                tmpNode = AddState(tmpNode, "initial");
                tmpNode = AddAction(tmpNode, name + "Action", "output");
                AddRef(tmpNode, "DataModel", message);
            }

        }
        
        public void AddOperation(string name, string message) 
        {
            AddOperation(name, "", "", message,"");
        }


        public void AddAttribute(string name, string type)
        {
           
        }

        public void AddCustomAttributeType(XmlNode node, string name, string type)
        {
            XmlNode tmpNode = XmlAddNode(node, "Custom");
            tmpNode.Attributes.Append(CreateAttribute("name", name));
            tmpNode.Attributes.Append(CreateAttribute("type", type));
        }

        public XmlNode AddCustomType(XmlNode node, string name, string type)
        {
            XmlNode tmpNode = XmlAddNode(node, "Custom");
            tmpNode.Attributes.Append(CreateAttribute("name", name));
            tmpNode.Attributes.Append(CreateAttribute("type", type)); 
            return tmpNode;
        }

        public void AddString(XmlNode node, string name, string value) 
        { 
           AddString(node, name, value, "1","1");  
        }

        public void AddString(XmlNode node, string name, string value, string min, string max) 
        {
            XmlNode tmpNode = XmlAddNode(node, "String");
            tmpNode.Attributes.Append(CreateAttribute("name", name));
            tmpNode.Attributes.Append(CreateAttribute("value", value));
            tmpNode.Attributes.Append(CreateAttribute("minOccurs", min));

            if (max == "unbounded")
                max = "99999"; 
            tmpNode.Attributes.Append(CreateAttribute("maxOccurs", max)); 
        }

        public XmlNode AddElement(string name, string refname)
        {
            XmlNode tmpNode = AddDataModel(peachNode, name, null);
            return tmpNode;
        }

        public XmlNode AddXMLElement(XmlNode node, string name)
        {
            return AddXMLElement(node, name, "","", null);
        }

        public XmlNode AddXMLElement(XmlNode node, string name, string refname)
        {
            XmlNode tmpNode = XmlAddNode(node, "XmlElement");
            tmpNode.Attributes.Append(CreateAttribute("name", name.Replace(":", "_")));
            tmpNode.Attributes.Append(CreateAttribute("ref", refname));
            return tmpNode; 
        }

        public XmlNode AddXMLElement( XmlNode node, string name, string nsurl, string prepend,  string value )
        {
            XmlNode tmpNode = XmlAddNode(node, "XmlElement");
            tmpNode.Attributes.Append(CreateAttribute("ns", nsurl));
            tmpNode.Attributes.Append(CreateAttribute("name", name.Replace(":", "_")));
            tmpNode.Attributes.Append(CreateAttribute("elementName", name));
            if (value != null)
                AddString(tmpNode, name.Replace(":","_") + "Value", value);
            return tmpNode; 
        }

        public XmlNode AddXMLAttribute( XmlNode node, string name, string prependns, string nsurl, string value )
        {
            //Die xml die 
            XmlNode tmpNode = XmlAddNode(node, "XmlAttribute");
            tmpNode.Attributes.Append(CreateAttribute("name", name.Replace(":", "_"))); 
            tmpNode.Attributes.Append(CreateAttribute("ns", nsurl));
            tmpNode.Attributes.Append(CreateAttribute("attributeName", name));
            tmpNode.Attributes.Append(CreateAttribute("minOccurs", "1" ));
            tmpNode.Attributes.Append(CreateAttribute("maxOccurs", "1" ));
            if (value != "")
                AddString(tmpNode, name.Replace(":", "_") + "Value", value);
            return tmpNode;
        } 

        public void AddBlob(XmlNode node, string name, string type)
        {
            XmlNode tmpNode = XmlAddNode(node, "Blob");
            tmpNode.Attributes.Append(CreateAttribute("name", name));
            tmpNode.Attributes.Append(CreateAttribute("valuetype", type));
        }

        public void AddNumber(XmlNode node, string name, string size)
        {
            AddNumber(node, name, size, "65");
        }

        public void AddNumber(XmlNode node, string name, string size, string value)
        {
            XmlNode tmpNode = XmlAddNode(node, "Number");
            tmpNode.Attributes.Append(CreateAttribute("name", name));
            tmpNode.Attributes.Append(CreateAttribute("size", size));
            tmpNode.Attributes.Append(CreateAttribute("value", value));
        }

        public string GetName(XmlQualifiedName qName )
        {
            if (qName == null)
                return "";

            string name = "";
            string[] splitUrl = qName.Namespace.Split('/');
 
            if(splitUrl != null){

                if (splitUrl.Last() == "XMLSchema" )
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

        private void SortDataModels(XmlNode node)
        {

            ArrayList nodeList = new ArrayList(); 

            //sigh  sorting by reference  
            foreach (XmlNode child in node.ChildNodes)
            {
                if (child.Name == "DataModel")
                {
                    Console.WriteLine("Name: " + (child.Attributes["name"]).Value);

                    foreach ( XmlNode element in child.ChildNodes )
                    {
                        if (element.Name == "XmlElement")
                        {
                            Console.WriteLine( "ElementName: " + (element.Attributes["name"]).Value);

                             if( (element.Attributes["ref"]) != null )
                                 Console.WriteLine( "Reference: " + (element.Attributes["ref"]).Value + "\n" );

                            nodeList.Insert( 0, element );
                        }
                    }
                }
            }
        }

        public void Output()
        {
            peachNode.AppendChild(runNode);
            SortDataModels(peachNode); 
            xdoc.Save(filename);
        }

        public XmlNode AddDataModel(XmlNode node, string name)
        {
            return AddDataModel(node, name, null);
        }

        public XmlNode AddDataModel(XmlNode node, string name, string refTarget)
        {
            XmlNode tmpNode = XmlAddNode(node, "DataModel");

            name = name.Replace(':', '_').Replace('.','_');

            if(name != null)
                tmpNode.Attributes.Append(CreateAttribute("name", name));
            
            if(refTarget != null)
                tmpNode.Attributes.Append(CreateAttribute("ref", refTarget));

            return tmpNode;
        }

        public XmlNode AddBlock(XmlNode node, string name)
        {
            return AddBlock(node, name, null);
        }

        public XmlNode AddBlock(XmlNode node, string name, string refTarget)
        {
            XmlNode tmpNode = XmlAddNode(node, "Block");

            if(name != null)
                tmpNode.Attributes.Append(CreateAttribute("name", name));

            if(refTarget != null)
                tmpNode.Attributes.Append(CreateAttribute("ref", refTarget));
            return tmpNode;
        }

        public XmlNode AddChoice(XmlNode node, string name)
        {
            return AddChoice(node, name, null);

        }

        public XmlNode AddChoice(XmlNode node, string name, string type)
        {
            XmlNode tmpNode = XmlAddNode(node, "Choice");
            tmpNode.Attributes.Append(CreateAttribute("name", name));
      //      tmpNode.Attributes.Append(CreateAttribute("type", type));
            return tmpNode;

        }

        public XmlNode AddAction(XmlNode node, string name, string type)
        {
            XmlNode tmpNode = XmlAddNode(node, "Action");
            if (tmpNode.Attributes != null)
            {
                tmpNode.Attributes.Append(CreateAttribute("name", name));
                tmpNode.Attributes.Append(CreateAttribute("type", type));
            }
            return tmpNode;
        }

        public XmlNode AddPublisher(XmlNode node, string name, string classname)
        {
            XmlNode tmpNode = XmlAddNode(node, "Publisher");
            if (tmpNode.Attributes != null)
            {
                tmpNode.Attributes.Append(CreateAttribute("name", name));
                tmpNode.Attributes.Append(CreateAttribute("class", classname));
            }
            return tmpNode;
        }


        public XmlNode AddRef(XmlNode node, string type, string refName){
            XmlNode tmpNode = XmlAddNode(node, type);
            tmpNode.Attributes.Append(CreateAttribute("ref", refName));
            return tmpNode;

        }

        public XmlNode AddTest(XmlNode node, string name){
            XmlNode tmpNode = XmlAddNode(node, "Test");
            tmpNode.Attributes.Append(CreateAttribute("name", name ));
            return tmpNode; 
        }
        public XmlNode AddParam(XmlNode node, string name, string value){
            XmlNode tmpNode = XmlAddNode(node, "Param");
            tmpNode.Attributes.Append(CreateAttribute("name", name));
            tmpNode.Attributes.Append(CreateAttribute("value", value));
            return tmpNode;

        }

        public XmlNode AddStateModel(string name , string initialState){
            return AddStateModel(peachNode, name, initialState);
        }

        public  XmlNode AddState(XmlNode node, string name){
            XmlNode tmpNode = XmlAddNode(node, "State");
            tmpNode.Attributes.Append(CreateAttribute("name", name ));
            return tmpNode;
        }

        public XmlNode AddStateModel(XmlNode node, string name, string initialState)
        {
            XmlNode tmpNode = XmlAddNode(node, "StateModel");
            tmpNode.Attributes.Append(CreateAttribute("name", name));
            tmpNode.Attributes.Append(CreateAttribute("initialState", initialState));
            return tmpNode;
        }

        public XmlNode XmlAddNode(XmlNode node, string type){
                XmlNode tmpNode = xdoc.CreateElement(type);
                node.AppendChild(tmpNode);
                return tmpNode;
        }
        
    }
}
