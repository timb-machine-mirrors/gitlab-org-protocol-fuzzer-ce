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
			Document document = new Document();

			try
			{
				PdfWriter writer = PdfWriter.GetInstance(document, 
					new FileStream(@"fuzzed.pdf", FileMode.Create));

				document.Open();
				document.Add(new Paragraph("fax"));
				string file = Directory.GetCurrentDirectory() + "\\fuzzed.tiff";
				file = file.Replace("\\", "/");
				document.Add(ImgCCITT.GetInstance(file));
				document.Close();
			}
			catch (DocumentException de)
			{
				Console.Error.WriteLine(de.Message);
			}
			catch (IOException ioe)
			{
				Console.Error.WriteLine(ioe.Message);
			}
		}
	}
}
