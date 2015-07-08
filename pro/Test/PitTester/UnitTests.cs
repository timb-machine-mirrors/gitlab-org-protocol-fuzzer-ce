using System.Collections.Generic;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using System.Text;
using Peach.Core;
using File = System.IO.File;
using Peach.Core.Test;

namespace PitTester
{
	[TestFixture]
	[Quick]
	class UnitTests : TestBase
	{
		// ValueSource attribute doesn't resolve properties
		// on the base class so expose the pit list here.
		public static IEnumerable<TestCase> AllPits
		{
			get { return ThePits; }
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
				PitTester.VerifyPitConfig(test.Pit.Versions[0]);
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
			var errors = new List<Exception>();

			for (int i = 0; i < test.Pit.Versions[0].Files.Count; ++i)
			{
				var fileName = test.Pit.Versions[0].Files[i].Name;

				try
				{
					PitTester.VerifyPit(LibraryPath, fileName, i == 0);
				}
				catch (Exception ex)
				{
					errors.Add(ex);
					Console.WriteLine("{0}", fileName);
					Console.WriteLine(ex.ToString());
				}
			}

			CollectionAssert.IsEmpty(errors);
		}

		[Test]
		public void TestIgnoreArrayField()
		{
			const string xml = @"
<Peach>
	<DataModel name='DM'>
		<String value='pre ' />
		<Block name='item' minOccurs='0'>
			<String name='value'/>
		</Block>
		<String value=' post' />
	</DataModel>

	<StateModel name='TheState' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='DM'/>
				<Data>
					<Field name='item[0].value' value='aaa' />
					<Field name='item[1].value' value='bbb' />
					<Field name='item[2].value' value='aaa' />
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
	<Ignore xpath='//value' />

	<Test name='Default'>
		<Open   action='TheState.Initial.Action' publisher='Pub'/>
		<Output action='TheState.Initial.Action' publisher='Pub'>
<![CDATA[
0000000: 7072 6520 6161 6161 6161 6161 6120 706f  pre aaaaaaaaa po
0000010: 7374                                     st
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
		public void TestSlurpChoiceOfArrayOfChoice()
		{
			const string xml = @"
<Peach>
	<DataModel name='DM'>
		<Choice name='c1' minOccurs='0'>
			<String name='str' value=' ' />
			<Block name='blk'>
				<Choice name='c2'>
					<String name='str' />
					<Block name='inner'>
						<String name='prefix' value='Hello' />
						<String name='tgt' value='World' />
					</Block>
				</Choice>
			</Block>
		</Choice>
	</DataModel>

	<StateModel name='TheState' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='DM'/>
				<Data>
					<Field name='c1[0].blk.c2.inner' value='' />
					<Field name='c1[1].str' value=' ' />
					<Field name='c1[2].blk.c2.inner.prefix' value='Foo' />
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
	<Slurp setXpath='//tgt' value='Hello' />

	<Test name='Default'>
		<Open   action='TheState.Initial.Action' publisher='Pub'/>
		<Output action='TheState.Initial.Action' publisher='Pub'>
<![CDATA[
0000000: 4865 6c6c 6f48 656c 6c6f 2046 6f6f 4865  HelloHello FooHe
0000010: 6c6c 6f                                  llo
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
		<Number name='num' size='8'>
			<Fixup class='SequenceRandom' />
		</Number>
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
	<Slurp setXpath='//Act1/DM/num' valueType='hex' value='00'/>
	<Slurp setXpath='//Act1/DM/str1' value='1234567890'/>
	<Slurp setXpath='//Act2/DM/num' value='0'/>
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
