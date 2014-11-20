using System.IO;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Analyzers;

namespace Peach.Pro.Test.Core.Publishers
{
	[TestFixture] [Category("Peach")]
	class FilePublisherTests
	{
		[Test]
		public void Test1()
		{
			string xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
				<Peach>
				   <DataModel name=""TheDataModel"">
				       <String value=""Hello""/>
				   </DataModel>

				   <StateModel name=""TheStateModel"" initialState=""InitialState"">
				       <State name=""InitialState"">
				           <Action name=""Action1"" type=""output"">
				               <DataModel ref=""TheDataModel""/>
				           </Action>
				       </State>
				   </StateModel>

				   <Test name=""Default"">
				       <StateModel ref=""TheStateModel""/>
				       <Publisher class=""File"">
				           <Param name=""FileName"" value=""{0}""/>
				       </Publisher>
				   </Test>

				</Peach>";

			string tempFile = Path.GetTempFileName();

			xml = string.Format(xml, tempFile);

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			RunConfiguration config = new RunConfiguration();
			config.singleIteration = true;

			Engine e = new Engine(null);
			e.startFuzzing(dom, config);

			string[] output = File.ReadAllLines(tempFile);

			Assert.AreEqual(1, output.Length);
			Assert.AreEqual("Hello", output[0]);
		}
	}
}
