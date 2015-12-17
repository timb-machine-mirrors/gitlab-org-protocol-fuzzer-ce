using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Test;

namespace PitTester
{
	[TestFixture]
	[Quick]
	internal class UnitTests
	{
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
				PitTester.TestPit("", pitFile, true, null, true);
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
				PitTester.TestPit("", pitFile, true, null, true);
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
				PitTester.TestPit("", pitFile, true, null, true);
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

			var ex = Assert.Throws<AggregateException>(() =>
			{
				try
				{
					PitTester.TestPit("", pitFile, true, null, true);
				}
				finally
				{
					File.Delete(pitFile);
					File.Delete(pitTest);
				}
			});

			var sb = new StringBuilder();
			ex.Handle(e =>
			{
				sb.AppendLine(e.Message);
				return true;
			});
			var err = sb.ToString();

			Assert.That(err, Is.StringStarting("Encountered an unhandled exception on iteration 1, seed "));
			Assert.That(err, Is.StringContaining("Missing record in test data"));
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
				PitTester.TestPit("", pitFile, false, null, true);
			}
			finally
			{
				File.Delete(pitFile);
				File.Delete(pitTest);
			}
		}

		[Test]
		public void SlurpOverField()
		{
			const string xml = @"
<Peach>
	<DataModel name='DM'>
		<String name='str1' value='0' />
	</DataModel>

	<StateModel name='TheState' initialState='Initial'>
		<State name='Initial'>
			<Action name='Act1' type='output'>
				<DataModel ref='DM'/>
				<Data>
					<Field name='str1' value='Hello'/>
				</Data>
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

	<Test name='Default'>
		<Open   action='TheState.Initial.Act1' publisher='Pub'/>
		<Output action='TheState.Initial.Act1' publisher='Pub'>
<![CDATA[
0000000: 3132 3334 3536 3738 3930                 1234567890
]]>
		</Output>
	</Test>
</TestData>
";

			var pitFile = Path.GetTempFileName();
			var pitTest = pitFile + ".test";

			File.WriteAllText(pitFile, xml);
			File.WriteAllText(pitTest, test);

			try
			{
				PitTester.TestPit("", pitFile, false, null, true);
			}
			finally
			{
				File.Delete(pitFile);
				File.Delete(pitTest);
			}
		}

		[Test]
		public void TestPitLintIgnore()
		{
			const string xml = @"<?xml version='1.0' encoding='utf-8'?>
<!--
PEACH PIT COPYRIGHT NOTICE AND LEGAL DISCLAIMER
-->
<Peach 
	xmlns='http://peachfuzzer.com/2012/Peach'
	xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance'
	xsi:schemaLocation='http://peachfuzzer.com/2012/Peach peach.xsd'
	author='Peach Fuzzer, LLC'
	description='PIT'>

	<DataModel name='DM'>
		<String name='str1' value='0' />
	</DataModel>

	<StateModel name='TheState' initialState='Initial'>
		<State name='Initial'>
			<!-- before -->
			<!-- PitLint: Skip_StartIterationEvent -->
			<!-- after -->
			<Action type='call' method='InitializeIterationEvent' publisher='Peach.Agent' />
			<Action type='call' method='StartIterationEvent' publisher='Peach.Agent' />
			<!-- PitLint: Allow_WhenControlIteration -->
			<Action name='Act1' type='output' when='context.controlIteration'>
				<DataModel ref='DM'/>
				<Data>
					<Field name='str1' value='Hello'/>
				</Data>
			</Action>
			<Action type='call' method='ExitIterationEvent' publisher='Peach.Agent'/>
		</State>
	</StateModel>

	<!-- PitLint: Skip_Lifetime -->
	<Test name='Default' maxOutputSize='65535' targetLifetime='iteration'>
		<StateModel ref='TheState'/>
		<Publisher class='RawEther' name='pub1'>
			<Param name='Interface' value='##Interface##'/>
			<!-- Pit is send only, don't need to expose timeouts or filter -->
			<!-- PitLint: Allow_MissingParamValue=Timeout -->
			<!-- PitLint: Allow_MissingParamValue=Filter -->
		</Publisher>
		<Publisher class='RawEther' name='pub2'>
			<!-- Pit is send only, don't need to expose timeouts or filter -->
			<!-- PitLint: Allow_MissingParamValue=Timeout -->
			<!-- PitLint: Allow_MissingParamValue=Filter -->
			<Param name='Interface' value='##Interface##'/>
		</Publisher>
		<Publisher class='Null' name='null'>
			<!-- Comment -->
			<!-- PitLint: Allow_MissingParamValue=MaxOutputSize -->
		</Publisher>
	</Test>
</Peach>
";

			const string config = @"<?xml version='1.0' encoding='utf-8'?>
<PitDefines>
</PitDefines>
";

			using (var tmp = new TempDirectory())
			{
				var pitFile = Path.Combine(tmp.Path, "foo.xml");
				File.WriteAllText(pitFile, xml);
				File.WriteAllText(Path.Combine(tmp.Path, "foo.xml.config"), config);
				PitTester.VerifyPit(tmp.Path, pitFile, true);
			}
		}

		[Test]
		public void TestNewlineInValue()
		{
			const string xml = @"<?xml version='1.0' encoding='utf-8'?>
<!--
PEACH PIT COPYRIGHT NOTICE AND LEGAL DISCLAIMER
-->
<Peach 
	xmlns='http://peachfuzzer.com/2012/Peach'
	xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance'
	xsi:schemaLocation='http://peachfuzzer.com/2012/Peach peach.xsd'
	author='Peach Fuzzer, LLC'
	description='PIT'>

	<DataModel name='DM'>
		<String name='str1' value='new
line' />
	</DataModel>

	<StateModel name='TheState' initialState='Initial'>
		<State name='Initial'>
			<Action type='call' method='StartIterationEvent' publisher='Peach.Agent' />
			<Action name='Act1' type='output'>
				<DataModel ref='DM'>
					<Blob name='blob' valueType='hex' value='00
11' />
					<String name='str2' value='another
new
line' />
				</DataModel>
				<Data>
					<Field name='str1' value='Bad
Field'/>
				</Data>
			</Action>
			<Action type='call' method='ExitIterationEvent' publisher='Peach.Agent'/>
		</State>
	</StateModel>

	<!-- PitLint: Skip_Lifetime -->
	<Test name='Default' maxOutputSize='65535' targetLifetime='iteration'>
		<StateModel ref='TheState'/>
		<Publisher class='Null' name='null'>
			<!-- Comment -->
			<!-- PitLint: Allow_MissingParamValue=MaxOutputSize -->
		</Publisher>
	</Test>
</Peach>
";

			const string config = @"<?xml version='1.0' encoding='utf-8'?>
<PitDefines>
</PitDefines>
";

			using (var tmp = new TempDirectory())
			{
				var pitFile = Path.Combine(tmp.Path, "foo.xml");
				File.WriteAllText(pitFile, xml);
				File.WriteAllText(Path.Combine(tmp.Path, "foo.xml.config"), config);
				
				var ex = Assert.Throws<ApplicationException>(() => PitTester.VerifyPit(tmp.Path, pitFile, true));

				StringAssert.Contains("Element has value attribute with embedded newline: <String name=\"str1\"", ex.Message);
				StringAssert.Contains("Element has value attribute with embedded newline: <String name=\"str2\"", ex.Message);
				StringAssert.Contains("Element has value attribute with embedded newline: <Field name=\"str1\"", ex.Message);
				StringAssert.DoesNotContain("Element has value attribute with embedded newline: <Blob name=\"blob\"", ex.Message);
			}
		}

		[Test]
		public void TestSkippedActions()
		{
			const string xml = @"
<Peach>
	<StateModel name='TheState' initialState='Initial'>
		<State name='Initial'>
			<Action type='output' when='context.controlIteration' >
				<DataModel name='DM'>
					<String value='output' />
				</DataModel>
			</Action>
			<Action type='input' when='context.controlIteration' >
				<DataModel name='DM'>
					<String value='input1' token='true' />
				</DataModel>
			</Action>
			<Action type='input'>
				<DataModel name='DM'>
					<String value='input2' token='true' />
				</DataModel>
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
	<Test name='Default'>
		<Open   action='TheState.Initial.Action' publisher='Pub'/>
		<Output action='TheState.Initial.Action' publisher='Pub'>
<![CDATA[
0000   6F 75 74 70 75 74                                 output
]]>
		</Output>
		<Input action='TheState.Initial.Action_1' publisher='Pub'>
<![CDATA[
0000   69 6E 70 75 74 31                                 input1
]]>
		</Input>
		<Input action='TheState.Initial.Action_2' publisher='Pub'>
<![CDATA[
0000   69 6E 70 75 74 32                                 input2
]]>
		</Input>
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
				PitTester.TestPit("", pitFile, false, null, false, 5);
			}
			finally
			{
				File.Delete(pitFile);
				File.Delete(pitTest);
			}
		}

		[Test]
		public void TestRecursiveActions()
		{
			const string xml = @"
<Peach>
	<StateModel name='TheState' initialState='Initial'>
		<State name='Initial'>
			<Action type='input'>
				<DataModel name='DM'>
					<Blob />
				</DataModel>
			</Action>

			<Action type='changeState' ref='Initial' when='state.actions[0].dataModel.Value.Length > 0' />
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
	<Test name='Default'>
		<Open   action='TheState.Initial.Action' publisher='Pub'/>
		<Input action='TheState.Initial.Action' publisher='Pub'>
<![CDATA[
0000   69 6E 70 75 74 31                                 input1
]]>
		</Input>
		<Input action='TheState.Initial.Action' publisher='Pub'>
<![CDATA[
0000   69 6E 70 75 74 32                                 input2
]]>
		</Input>
		<Input action='TheState.Initial.Action' publisher='Pub'>
<![CDATA[
]]>
		</Input>
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
				PitTester.TestPit("", pitFile, false, null, false, 5);
			}
			finally
			{
				File.Delete(pitFile);
				File.Delete(pitTest);
			}
		}

	}
}
