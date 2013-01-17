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
			Document.Compress = false;

			try
			{
				byte[] data;

				PdfWriter writer = PdfWriter.GetInstance(document, 
					new FileStream(@"fuzzed.pdf", FileMode.Create));

				document.Open();
				document.Add(new Paragraph("GIF"));
				string file = Directory.GetCurrentDirectory() + "\\fuzzed.gif";
				file = file.Replace("\\", "/");

				//FileStream fin = File.Open(file, FileMode.Open);
				//data = new byte[fin.Length];
				//fin.Read(data, 0, data.Length);
				//fin.Close();

				//document.Add(new ImgRaw(128, 128, 6, 8, data));

				document.Add(Image.GetInstance(file));
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
