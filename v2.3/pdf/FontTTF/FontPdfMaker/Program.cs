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
				document.Add(new Paragraph("1"));

				try
				{
					BaseFont bfComic = BaseFont.CreateFont("fuzzed.ttf", BaseFont.CP1252, BaseFont.EMBEDDED);
					Font font = new Font(bfComic, 12);
					String text1 = "This is the quite popular True Type font 'Peach'.";
					document.Add(new Paragraph(text1, font));
				}
				catch(Exception e)
				{
					Console.Error.WriteLine(e.Message);
				}

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
			catch (Exception e)
			{
				Console.Error.WriteLine(e.Message);
			}
		}
	}
}
