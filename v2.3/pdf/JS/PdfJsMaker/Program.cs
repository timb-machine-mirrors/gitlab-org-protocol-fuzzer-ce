using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.text.factories;
using iTextSharp.text.exceptions;
using iTextSharp.text.error_messages;

namespace PdfJsMaker
{
	class Program
	{
		static void Main(string[] args)
		{
			string fileJs = null;

			using (StreamReader fin = File.OpenText("fuzzer_test_case.js"))
				fileJs = fin.ReadToEnd();

			string javascript = fileJs + "fuzz_me();";

			Document document = new Document();
			try
			{
				PdfWriter writer = PdfWriter.GetInstance(document, 
					new FileStream(@"testcase.pdf", FileMode.Create));

				document.Open();

				PdfAction jAction = PdfAction.JavaScript(javascript, writer);
				writer.AddJavaScript(jAction);
				document.Add(new Paragraph(" "));
			}
			catch (DocumentException de)
			{
				Console.Error.WriteLine(de.Message);
			}
			catch (IOException ioe)
			{
				Console.Error.WriteLine(ioe.Message);
			}

			document.Close();

		}
	}
}
