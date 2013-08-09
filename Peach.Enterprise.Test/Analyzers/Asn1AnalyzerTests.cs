using System;
using NUnit.Framework;
using System.Reflection;
using System.IO;
using Asn1Lib;
using System.Text;

namespace Peach.Enterprise.Test.Analyzers
{
	[TestFixture]
	class Asn1AnalyzerTests
	{
		static MemoryStream googleDer = LoadResource("google.der");

		[SetUp]
		public void SetUp()
		{
			googleDer.Seek(0, SeekOrigin.Begin);
		}

		[Test]
		public void Test1()
		{
			var der = Asn1.Decode(googleDer);

			var ms = new MemoryStream();

			Asn1.ExportText(ms, der);

			var final = Encoding.ASCII.GetString(ms.ToArray());

			Console.WriteLine(final);

			Assert.NotNull(der);
		}

		private static MemoryStream LoadResource(string name)
		{
			var asm = Assembly.GetExecutingAssembly();
			var res = asm.GetName().Name + "." + name;
			using (var stream = asm.GetManifestResourceStream(res))
			{
				var ms = new MemoryStream();
				stream.CopyTo(ms);
				return ms;
			}
		}

	}
}
