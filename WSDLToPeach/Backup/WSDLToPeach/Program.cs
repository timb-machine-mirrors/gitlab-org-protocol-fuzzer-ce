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
    class Program
    {

        public static void Usage()
        {
            Console.WriteLine("Usage: WSDLToPeach.exe file.wsdl peach.xml");
            Console.WriteLine("Debugging: WSDLToPeach.exe file.wsdl peach.xml <stage>\n");
            Console.WriteLine("Stage: single stage code, multiple codes do not stack");
            Console.WriteLine("      : b import files no processing");
            Console.WriteLine("      : t Process Types");
            Console.WriteLine("      : p Process Ports");
            Console.WriteLine("      : m Process Messages");
            Console.WriteLine("      : s Process Services");
        }

        public static void Main(string[] args)
        {

            if (args.Length < 2)
            {
                Usage();
                return;
            }

            String wsdlFileName = args[0];
            String peachFileName = args[1];
            String stage = ""; 
            if( args.Length > 2) 
             stage = args[2]; 


            WSDLParser parser = new WSDLParser( wsdlFileName, peachFileName);
            parser.go(stage);
        }
    }
}