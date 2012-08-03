//Author contact: Thanh.Dao@gmx.net
using System;
using System.Windows.Forms;
using System.Text.RegularExpressions;

using System.Drawing;
namespace WSDLParser
{
	public class MyRichText : RichTextBox
	{
		public bool IsEmpty=false;
		public static String [] keywords={																	   "anyURI",
											 "base64Binary",
											 "boolean",
											 "byte",
											 "date",
											 "dateTime",
											 "decimal",
											 "double",
											 "duration",
											 "ENTITIY",
											 "float",
											 "gDay",
											 "gMonth",
											 "gMonthDay",
											 "gYear",
											 "gYearMonth",
											 "hexBinary",
											 "ID",
											 "IDREF",
											 "integer",
											 "int",
											 "language",
											 "long",
											 "Name",
											 "NCName",
											 "negativeInteger",
											 "NMTOKEN",
											 "nonNegativeInteger",
											 "nonPositiveInteger",
											 "normalizedString",
											 "NOTATION",
											 "positiveInteger",
											 "QName",
											 "short",
											 "string",
											 "time",
											 "token",
											 "unsignedByte",
											 "unsignedInt",
											 "unsignedLong",
											 "unsignedShort",
											 "XmlSchemaElement",
											 "XmlSchemaAttribute"};
	
		public static String [] operators={"<","/",">","?"}
							;

	
		public MyRichText()
		{
			this.AllowDrop=true;
			MyInit();

		}

		private void MyInit()
		{
			
		}


		protected override void OnDragEnter(DragEventArgs drgevent)
		{
			base.OnDragEnter (drgevent);

			if (drgevent.Data.GetDataPresent(DataFormats.Text))  
				drgevent.Effect=DragDropEffects.Copy;  
			else  
				drgevent.Effect=DragDropEffects.None; 
 

		}
		
		
		void ParseLine(string line) 
		{
			//Ignore the case whitespace character
			Regex r=new Regex("([ \\t{}<>():;=&\"])");
			String [] tokens=r.Split(line); 
			// Check whether the token is a keyword. 

			foreach (string token in tokens) 
			{ 
				// Set the tokens default color and font.				
				this.SelectionColor=Color.Black;
				this.SelectionFont=new Font("Courier New", 10, 
					FontStyle.Regular); 

				bool found=false;
				for (int i=0; i < keywords.Length; i++) 
				{
					if (keywords[i] == token) 
					{
						// Apply alternative color and font to highlight keyword.
						this.SelectionColor=Color.Blue;
						this.SelectionFont=new Font("Courier New", 10,
							FontStyle.Bold);
						found=true;
						break;
					}
				}
				for (int i=0; !found && i < operators.Length; i++) 
				{
					if (operators[i] == token) 
					{
						// Apply alternative color and font to highlight keyword.
						this.SelectionColor=Color.Red ;
						this.SelectionFont=new Font("Courier New", 10,
							FontStyle.Bold);
								
						break;
					}
				}

				this.SelectedText=token;				
			} 
			this.SelectedText =this.SelectedText + Environment.NewLine ;
		} 



		public void HighLightSyntax(string input)
		{			
			if (input == null || input =="") return;
			Regex r=new Regex("\\n");
			String [] lines=r.Split(input);
			foreach (string l in lines) 
			{
				ParseLine(l);
			}
			
		}

		protected override void OnTextChanged(EventArgs e)
		{			
			base.OnTextChanged (e);			

			string str=this.Text;

			IsEmpty=(str == null || str == string.Empty);
			
			int i= this.SelectionStart ;
			this.Text="";
			HighLightSyntax (str);	
			this.SelectionStart=i;
		}



	}
}
