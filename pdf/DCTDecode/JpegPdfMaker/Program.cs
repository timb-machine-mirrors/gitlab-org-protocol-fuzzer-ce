using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
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
			MakePdf(args[0]);
		}

		static void MakePdf(string orig)
		{
			Document document = new Document();

			while (File.Exists("fuzzed.pdf"))
			{
				Console.Error.Write('.');
				try
				{
					File.Delete("fuzzed.pdf");
				}
				catch (IOException)
				{
				}

				Thread.Sleep(100);
			}

			try
			{
				PdfWriter writer = PdfWriter.GetInstance(document,
					new FileStream(@"fuzzed.pdf", FileMode.Create));

				document.Open();

				document.Add(new Paragraph("jpeg image"));
				byte[] data;

				using (FileStream fin = new FileStream(orig, FileMode.Open))
				{
					data = new byte[fin.Length];
					fin.Read(data, 0, data.Length);
				}

				JpegFull imgOrigional = new JpegFull(data);

				using (FileStream fin = new FileStream("fuzzed.jpg", FileMode.Open))
				{
					data = new byte[fin.Length];
					fin.Read(data, 0, data.Length);
				}

				Jpeg img = new Jpeg(data, imgOrigional);

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
