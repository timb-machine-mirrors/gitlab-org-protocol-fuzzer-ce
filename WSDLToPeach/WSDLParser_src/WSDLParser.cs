//Author contact: Thanh.Dao@gmx.net
using System.Web.Services.Description;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;
using Binding = System.Web.Services.Description.Binding;
using Message = System.Web.Services.Description.Message;

namespace WSDLParser
{

	public class WSDLParser 
	{		
		public TreeNode ServiceNode;								
		private XmlSchemas _schemas;
		private ServiceDescriptionCollection _services=new ServiceDescriptionCollection() ;		
		
		public WSDLParser(ServiceDescription service)
		{						
			_services.Add(service) ;
			_schemas=service.Types.Schemas ;

			if (service.Name == string.Empty) service.Name=service.RetrievalUrl;
			if (service.Name == string.Empty) service.Name=service.TargetNamespace;

			ServiceNode=new TreeNode(service.Name);		
			ServiceNode.Tag=service.Documentation ;
			ServiceNode.ImageIndex=15;
			ServiceNode.SelectedImageIndex=15;

			Parse();
			
			ServiceNode.Expand ();			
		}

		private string GetProtocol (Binding binding)
		{
			if (binding.Extensions.Find (typeof(SoapBinding)) != null) return "Soap";
			HttpBinding hb = (HttpBinding) binding.Extensions.Find (typeof(HttpBinding));						
			if (hb == null) return "";
			if (hb.Verb == "POST") return "HttpPost";
			if (hb.Verb == "GET") return "HttpGet";
			return "";
		}
		
	private void GetOperationFormat(OperationBinding obin, Operation oper, out SoapBindingStyle style,out SoapBindingUse inputUse, out SoapBindingUse outputUse, out string requestMessage, out string responseMessage, out TreeNode requestNode,out TreeNode responseNode)
		{
			style=SoapBindingStyle.Document;
			outputUse=SoapBindingUse.Literal;
			inputUse=SoapBindingUse.Literal;
			requestMessage=string.Empty;
			responseMessage=string.Empty;
			requestNode=null;			
			responseNode=null;
			SoapBindingStyle pStyle;
			SoapBindingUse pInputUse, pOutputUse;

			GetOperationFormat (obin, out pStyle, out pInputUse, out pOutputUse  );
			if (oper.Messages.Input != null)
			{
				requestNode=MessageToTreeNode(oper.Messages.Input,  pInputUse);
			}			
			if (oper.Messages.Output != null)
			{
				responseNode=MessageToTreeNode(oper.Messages.Output, pOutputUse  );
			}
												
			style=pStyle;
			outputUse=pOutputUse;
			inputUse=pInputUse;
		}

		
		void GetOperationFormat (OperationBinding obin, out SoapBindingStyle style, out SoapBindingUse inputUse, out SoapBindingUse outputUse)
		{		
			style=SoapBindingStyle.Document;
			inputUse=SoapBindingUse.Literal;
			outputUse=SoapBindingUse.Literal;
			if (obin.Extensions != null)
			{
				SoapOperationBinding sob=obin.Extensions.Find (typeof(SoapOperationBinding)) as SoapOperationBinding;
				if (sob != null) 
				{
					style = sob.Style;
					if (obin.Input != null)
					{
						SoapBodyBinding sbb0 = obin.Input.Extensions.Find (typeof(SoapBodyBinding)) as SoapBodyBinding;
						if (sbb0 != null) 
							inputUse = sbb0.Use;
					}

					if (obin.Output != null)
					{
						SoapBodyBinding sbb1 = obin.Output.Extensions.Find (typeof(SoapBodyBinding)) as SoapBodyBinding;
						if (sbb1 != null)
							outputUse = sbb1.Use;
					}
				}
			}
		}


		private TreeNode MessageToTreeNode(OperationMessage omsg, SoapBindingUse use)
		{			
			Message msg=_services.GetMessage (omsg.Message);
			
			TreeNode node=new TreeNode() ;
			SchemaParser ngen=new SchemaParser(_schemas);

			ngen.BindingUse=use;

			foreach (MessagePart part in msg.Parts)
			{
				if (part.Element == XmlQualifiedName.Empty)
				{
					TreeNode partNode=ngen.Translate(part.Type);
					partNode.ImageIndex=5;
					partNode.SelectedImageIndex=5;

					partNode.Text=part.Name;
					node.Nodes.Add(partNode);
				}
				else
				{
					TreeNode partNode=ngen.Translate(part.Element);
					partNode.ImageIndex=5;
					partNode.SelectedImageIndex=5;

					partNode.Text=part.Name;
					node.Nodes.Add(partNode);
				}

			}

			return node;			
		}



		public TreeNode TranslateOperation(Port port, OperationBinding obin, Operation oper, string protocol)
		{
			TreeNode tnOperation=new TreeNode (oper.Name, 13 , 13);
			SoapBindingStyle style=new SoapBindingStyle() ;
			SoapBindingUse inputUse=new SoapBindingUse() ;
			SoapBindingUse outputUse=new SoapBindingUse() ;

			string requestmsg=string.Empty;
			string responsemsg=string.Empty;
			TreeNode tnInput=new TreeNode ();
			TreeNode tnOutput=new TreeNode ();			
			TreeNode tnFault=new TreeNode ("Faults");									
			
			GetOperationFormat (obin, oper, out style,out inputUse,out outputUse, out requestmsg, out responsemsg,out tnInput, out tnOutput);
 
			string operDesc=string.Empty;
			operDesc +=oper.Documentation + "\n";
			operDesc +="Style: " + style.ToString() + "\n" ;

			tnOperation.Tag=operDesc;
			

			MessageCollection messages=_services[0].Messages;			
			if (oper.Messages.Input != null)
			{
				Message messageIn=messages[oper.Messages.Input.Message.Name] ;			
				if (messageIn != null )
				{					
					
					if (tnInput == null) tnInput=new TreeNode() ;			
					tnInput.Tag=requestmsg;	
					tnInput.ImageIndex=13;
					tnInput.SelectedImageIndex=13;

					if (oper.Messages.Input.Name != null && oper.Messages.Input.Name != string.Empty)
					{
						tnInput.Text="Input: " + oper.Messages.Input.Name ;
					}
					else
						tnInput.Text="Input: " + oper.Messages.Input.Message.Name ;

					
					if (tnInput != null) tnOperation.Nodes.Add  (tnInput );
				};
			};

			if (oper.Messages.Output != null)
			{
				Message messageOut=messages[oper.Messages.Output.Message.Name] ;
				if (messageOut != null )
				{				

					if (tnOutput == null) tnOutput=new TreeNode() ;	
					tnOutput.Tag=responsemsg;
					tnOutput.ImageIndex=13;
					tnOutput.SelectedImageIndex=13;

					if (oper.Messages.Output.Name != null && oper.Messages.Output.Name != string.Empty)
					{
						tnOutput.Text="Output: " + oper.Messages.Output.Name ;
					}
					else
						tnOutput.Text="Output: " + oper.Messages.Output.Message.Name ;
					
					if (tnOutput != null) tnOperation.Nodes.Add  (tnOutput );
				};

			};
		
			foreach (OperationFault faultOp in  oper.Faults)
			{				
				Message messageFault=messages[faultOp.Message.Name] ;
				if (messageFault != null )				
				{					

					TreeNode treeNode=new TreeNode() ;			

					tnFault.ImageIndex=14;
					tnFault.SelectedImageIndex=14;
					if (treeNode != null)
						tnFault.Nodes.Add (treeNode);
				};

			};

			if (tnFault.Nodes.Count > 0 ) tnOperation.Nodes.Add  (tnFault );

			return tnOperation;			
			
		}
		
		public void Parse()
		{			
			foreach (Service service in _services[0].Services )
			{
				TreeNode tnService=new TreeNode(service.Name) ;
				tnService.ImageIndex=1;
				tnService.SelectedImageIndex=1;

				foreach (Port port in service.Ports)
				{
					XmlQualifiedName bindName=port.Binding ;
					Binding bind=_services.GetBinding(bindName);				
					PortType portType=_services.GetPortType(bind.Type) ;

					TreeNode tnPort=new TreeNode (port.Name);
					tnPort.ImageIndex=6;
					tnPort.SelectedImageIndex=6;

					string protocol=GetProtocol (bind);
					string portDesc="Protocol: " + protocol + "\n";

					switch (protocol) 
					{
						case "Soap":
							{
								SoapAddressBinding ad=(SoapAddressBinding)port.Extensions.Find(typeof(SoapAddressBinding));
								portDesc += "Location: " + ad.Location + "\n";
								break;
							}
						case "HttpGet":
							{
								HttpAddressBinding ad=(HttpAddressBinding)port.Extensions.Find(typeof(HttpAddressBinding));
								portDesc += "Location: " + ad.Location + "\n";
								break;
							}

						case "HttpPost":
							{
								HttpAddressBinding ad=(HttpAddressBinding)port.Extensions.Find(typeof(HttpAddressBinding));
								portDesc += "Location: "   + ad.Location + "\n";
								break;
							}
					}
					
					tnPort.Tag=portDesc;
					foreach (OperationBinding obin in bind.Operations)					
					{							 								
						foreach (Operation oper in portType.Operations)
						if (obin.Name.Equals(oper.Name) )
						{
							TreeNode tnOper=TranslateOperation (port, obin, oper, protocol);										
							tnOper.ImageIndex=11;
							tnOper.SelectedImageIndex=11;

							if (tnOper != null)
							{
								tnPort.Nodes.Add (tnOper);						
							};
						}																			
					}
			
					tnPort.Expand ();					
					tnService.Nodes.Add(tnPort) ;
				}

				ServiceNode.Nodes.Add (tnService);										
			}
		}		
	}
}
