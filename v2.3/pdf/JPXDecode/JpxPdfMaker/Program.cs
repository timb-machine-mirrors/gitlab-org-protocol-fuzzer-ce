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

				document.Add(new Paragraph("jpeg2k image"));
				byte[] data;

				using (FileStream fin = new FileStream(args[0], FileMode.Open))
				{
					data = new byte[fin.Length];
					fin.Read(data, 0, data.Length);
				}

				Jpeg2000Full origImage = new Jpeg2000Full(data);

				using (FileStream fin = new FileStream("fuzzed.jp2", FileMode.Open))
				{
					data = new byte[fin.Length];
					fin.Read(data, 0, data.Length);
				}

				Image img = new Jpeg2000(data, origImage);
				document.Add(img);
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
