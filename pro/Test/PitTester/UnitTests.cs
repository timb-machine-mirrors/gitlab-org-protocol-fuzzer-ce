using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Peach.Core;
using Peach.Pro.Core.WebServices;
using Peach.Pro.Core.WebServices.Models;
using File = System.IO.File;

namespace PitTester
{
	[TestFixture]
	class UnitTests
	{
		public class TestCase
		{
			public Pit Pit
			{
				get;
				set;
			}

			public override string ToString()
			{
				return Pit.Name;
			}
		}

		static UnitTests()
		{
			LibraryPath = "../../../pits/pro".Replace('/', Path.DirectorySeparatorChar);

			var lib = new PitDatabase(LibraryPath);
			var errors = new StringBuilder();

			lib.ValidationEventHandler += delegate(object sender, ValidationEventArgs e)
			{
				errors.AppendLine(e.FileName);
				errors.AppendLine(e.Exception.Message);
				errors.AppendLine();
			};

			// Ignore user pits & category "Test"
			AllPits = lib.Entries.Where(e => e.Locked && e.Tags.All(t => t.Name != "Category.Test")).Select(p => new TestCase() { Pit = p }).ToList();

			LoadErrors = errors.ToString();
		}

		public static string LibraryPath
		{
			get;
			private set;
		}

		public static IEnumerable<TestCase> AllPits
		{
			get;
			private set;
		}

		public static string LoadErrors
		{
			get;
			private set;
		}

		[Test]
		public void LoadLibrary()
		{
			if (!string.IsNullOrEmpty(LoadErrors))
				Assert.Fail(LoadErrors);

			Assert.Greater(AllPits.Count(), 0);
		}

		[Test]
		public void VerifyConfig([ValueSource("AllPits")]TestCase test)
		{
			var errors = new StringBuilder();
			var fileName = test.Pit.Versions[0].Files[0].Name;

			try
			{
				PitTester.VerifyPitConfig(fileName);
			}
			catch (Exception ex)
			{
				errors.AppendFormat("{0}.config", fileName);
				errors.AppendLine();
				errors.AppendLine(ex.Message);
			}

			if (errors.Length > 0)
				Assert.Fail(errors.ToString());
		}

		[Test]
		public void Verify([ValueSource("AllPits")]TestCase test)
		{
			var errors = new StringBuilder();

			for (int i = 0; i < test.Pit.Versions[0].Files.Count; ++i)
			{
				var fileName = test.Pit.Versions[0].Files[i].Name;

				try
				{
					PitTester.VerifyPit(LibraryPath, fileName, i == 0);
				}
				catch (Exception ex)
				{
					errors.AppendFormat("{0}", fileName);
					errors.AppendLine();
					errors.AppendLine(ex.Message);
				}
			}

			if (errors.Length > 0)
				Assert.Fail(errors.ToString());
		}

		[Test]
		public void Run([ValueSource("AllPits")]TestCase test)
		{
			var fileName = test.Pit.Versions[0].Files[0].Name;

			try
			{
				PitTester.TestPit(LibraryPath, fileName, false, null);
			}
			catch (FileNotFoundException)
			{
				Assert.Ignore("No test definition found.");
			}
			catch (Exception ex)
			{
				Assert.Fail(ex.Message);
			}
		}

		[Test]
		public void TestIgnoreChoice()
		{
			const string xml = @"
<Peach>
	<DataModel name='DM'>
		<Choice name='c'>
			<String name='str1' />
			<String name='str2' />
		</Choice>
		<String value='\r\n' />
	</DataModel>

	<StateModel name='TheState' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='DM'/>
				<Data>
					<Field name='c.str1' value='String One' />
				</Data>
				<Data>
					<Field name='c.str2' value='String Two' />
				</Data>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='TheState'/>
		<Publisher name='Pub' class='Null'/>
	</Test>
</Peach>
";

			const string test = @"
<TestData>
	<Ignore xpath='//str1' />
	<Ignore xpath='//str2' />

	<Test name='Default'>
		<Open   action='TheState.Initial.Action' publisher='Pub'/>
		<Output action='TheState.Initial.Action' publisher='Pub'>
<![CDATA[
0000   00 00 00 00 00 00 00 00 00 00 0d 0a              ............
]]>
		</Output>
		<Close  action='TheState.Initial.Action' publisher='Pub'/>
	</Test>
</TestData>
";

			// Ensure we can run when there is an ignore that matches a de-selected choice
			var pitFile = Path.GetTempFileName();
			var pitTest = pitFile + ".test";

			File.WriteAllText(pitFile, xml);
			File.WriteAllText(pitTest, test);

			try
			{
				PitTester.TestPit("", pitFile, true, null);
			}
			finally
			{
				File.Delete(pitFile);
				File.Delete(pitTest);
			}
		}

		[Test]
		public void UnhandledException()
		{
			const string xml = @"
<Peach>
	<DataModel name='DM'>
		<String name='str1' />
	</DataModel>

	<StateModel name='TheState' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='DM'/>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='TheState'/>
		<Publisher name='Pub' class='Tcp'>
			<Param name='Host' value='localhost' />
			<Param name='Port' value='65500' />
		</Publisher>
	</Test>
</Peach>
";

			const string test = @"
<TestData>
	<Test name='Default'>
		<Open   action='TheState.Initial.Action' publisher='Pub'/>
	</Test>
</TestData>
";

			// Ensure we can run when there is an ignore that matches a de-selected choice
			var pitFile = Path.GetTempFileName();
			var pitTest = pitFile + ".test";

			File.WriteAllText(pitFile, xml);
			File.WriteAllText(pitTest, test);

			var ex = Assert.Throws<PeachException>(() =>
			{
				try
				{
					PitTester.TestPit("", pitFile, true, null);
				}
				finally
				{
					File.Delete(pitFile);
					File.Delete(pitTest);
				}
			});

			Assert.That(ex.Message, Is.StringStarting("Encountered an unhandled exception on iteration 1"));
		}

		[Test]
		public void SlurpStringRandomFixup()
		{
			const string xml = @"
<Peach>
	<DataModel name='DM'>
		<String name='str1' value='0'>
			<Fixup class='SequenceRandom' />
		</String>
		<Number size='8' value='0' />
	</DataModel>

	<StateModel name='TheState' initialState='Initial'>
		<State name='Initial'>
			<Action name='Act1' type='output'>
				<DataModel ref='DM'/>
			</Action>
			<Action name='Act2' type='output'>
				<DataModel ref='DM'/>
			</Action>
		</State>
	</StateModel>

	<Test name='Default' maxOutputSize='65535'>
		<StateModel ref='TheState'/>
		<Publisher name='Pub' class='Null' />
	</Test>
</Peach>
";

			const string test = @"
<TestData>
	<Slurp setXpath='//Act1/DM/str1' value='1234567890'/>
	<Slurp setXpath='//Act2/DM/str1' value='31337'/>

	<Test name='Default'>
		<Open   action='TheState.Initial.Act1' publisher='Pub'/>
		<Output action='TheState.Initial.Act1' publisher='Pub'>
<![CDATA[
0000000: 3132 3334 3536 3738 3930 00              1234567890.
]]>
		</Output>
		<Output action='TheState.Initial.Act2' publisher='Pub'>
<![CDATA[
0000000: 3331 3333 3700                           31337.
]]>
		</Output>
	</Test>
</TestData>
";

			// Ensure we can run when there is an ignore that matches a de-selected choice
			var pitFile = Path.GetTempFileName();
			var pitTest = pitFile + ".test";

			File.WriteAllText(pitFile, xml);
			File.WriteAllText(pitTest, test);

			try
			{
				PitTester.TestPit("", pitFile, false, null);
			}
			finally
			{
				File.Delete(pitFile);
				File.Delete(pitTest);
			}
		}
	}
}
