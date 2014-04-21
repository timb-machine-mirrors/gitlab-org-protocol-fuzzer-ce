using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.Analyzers;

namespace Peach.Enterprise.Test.Fixups
{
	[TestFixture]
	class SecureRandomNumberFixupTests : Peach.Core.Test.DataModelCollector
	{
		[Test]
		public void Test1()
		{
			// standard test
			string xml = @"
				<Peach>
					<DataModel name='TheDataModel'>
						<Blob name='Random'>
							<Fixup class='SecureRandomNumber'>
								<Param name='ref' value='Random'/>
								<Param name='Length' value='10'/>
							</Fixup>
						</Blob>
					</DataModel>
	
					<StateModel name='TheState' initialState='Initial'>
						<State name='Initial'>
							<Action type='output'>
								<DataModel ref='TheDataModel'/>
							</Action>
						</State>
					</StateModel>
					
					<Test name='Default'>
						<StateModel ref='TheState'/>
						<Publisher class='Null'/>
					</Test>
				</Peach>";

			PitParser parser = new PitParser();

			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(Peach.Core.ASCIIEncoding.ASCII.GetBytes(xml)));

			RunConfiguration config = new RunConfiguration();
			config.singleIteration = true;

			Engine e = new Engine(null);
			e.startFuzzing(dom, config);

			Assert.AreEqual(1, values.Count);
			Assert.AreEqual(10, values[0].Length);
		}

		[Test]
		public void SizedElementTest()
		{
			string xml = @"
				<Peach>
					<DataModel name='TheDataModel'>
						<Blob name='Random' length='10'>
							<Fixup class='SecureRandomNumber'>
								<Param name='ref' value='Random'/>
								<Param name='Length' value='10'/>
							</Fixup>
						</Blob>
					</DataModel>
	
					<StateModel name='TheState' initialState='Initial'>
						<State name='Initial'>
							<Action type='output'>
								<DataModel ref='TheDataModel'/>
							</Action>
						</State>
					</StateModel>
					
					<Test name='Default'>
						<StateModel ref='TheState'/>
						<Publisher class='Null'/>
					</Test>
				</Peach>";

			PitParser parser = new PitParser();

			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(Peach.Core.ASCIIEncoding.ASCII.GetBytes(xml)));

			RunConfiguration config = new RunConfiguration();
			config.singleIteration = true;

			Engine e = new Engine(null);
			e.startFuzzing(dom, config);

			Assert.AreEqual(1, values.Count);
			Assert.AreEqual(10, values[0].Length);
			Assert.AreEqual(1, dataModels.Count);
			Assert.AreEqual(10, dataModels[0].find("Random").length);
		}

		[Test]
		public void LengthLessThanElementSizeTest()
		{
			string xml = @"
				<Peach>
					<DataModel name='TheDataModel'>
						<Blob name='Random' length='10'>
							<Fixup class='SecureRandomNumber'>
								<Param name='ref' value='Random'/>
								<Param name='Length' value='5'/>
							</Fixup>
						</Blob>
					</DataModel>
	
					<StateModel name='TheState' initialState='Initial'>
						<State name='Initial'>
							<Action type='output'>
								<DataModel ref='TheDataModel'/>
							</Action>
						</State>
					</StateModel>
					
					<Test name='Default'>
						<StateModel ref='TheState'/>
						<Publisher class='Null'/>
					</Test>
				</Peach>";

			PitParser parser = new PitParser();

			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(Peach.Core.ASCIIEncoding.ASCII.GetBytes(xml)));

			RunConfiguration config = new RunConfiguration();
			config.singleIteration = true;

			Engine e = new Engine(null);
			e.startFuzzing(dom, config);

			Assert.AreEqual(1, values.Count);
			Assert.AreEqual(5, values[0].Length);
			Assert.AreEqual(1, dataModels.Count);
			Assert.AreEqual(10, dataModels[0].find("Random").length);
		}

		[Test, ExpectedException(typeof(PeachException), ExpectedMessage = "Length is greater than 'ref' elements size.")]
		public void ElementTooSmallTest()
		{
			string xml = @"
				<Peach>
					<DataModel name='TheDataModel'>
						<Blob name='Random' length='10'>
							<Fixup class='SecureRandomNumber'>
								<Param name='ref' value='Random'/>
								<Param name='Length' value='12'/>
							</Fixup>
						</Blob>
					</DataModel>
	
					<StateModel name='TheState' initialState='Initial'>
						<State name='Initial'>
							<Action type='output'>
								<DataModel ref='TheDataModel'/>
							</Action>
						</State>
					</StateModel>
					
					<Test name='Default'>
						<StateModel ref='TheState'/>
						<Publisher class='Null'/>
					</Test>
				</Peach>";

			PitParser parser = new PitParser();

			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(Peach.Core.ASCIIEncoding.ASCII.GetBytes(xml)));

			RunConfiguration config = new RunConfiguration();
			config.singleIteration = true;

			Engine e = new Engine(null);
			e.startFuzzing(dom, config);
		}

		[Test, ExpectedException(typeof(PeachException), ExpectedMessage = "Error, unable to create instance of 'Fixup' named 'SecureRandomNumber'.\nExtended error: Exception during object creation: The length must be greater than 0.")]
		public void LengthLessThanZeroTest()
		{
			string xml = @"
				<Peach>
					<DataModel name='TheDataModel'>
						<Blob name='Random' length='10'>
							<Fixup class='SecureRandomNumber'>
								<Param name='ref' value='Random'/>
								<Param name='Length' value='-1'/>
							</Fixup>
						</Blob>
					</DataModel>
	
					<StateModel name='TheState' initialState='Initial'>
						<State name='Initial'>
							<Action type='output'>
								<DataModel ref='TheDataModel'/>
							</Action>
						</State>
					</StateModel>
					
					<Test name='Default'>
						<StateModel ref='TheState'/>
						<Publisher class='Null'/>
					</Test>
				</Peach>";

			PitParser parser = new PitParser();

			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(Peach.Core.ASCIIEncoding.ASCII.GetBytes(xml)));

			RunConfiguration config = new RunConfiguration();
			config.singleIteration = true;

			Engine e = new Engine(null);
			e.startFuzzing(dom, config);
		}
	}
}

// end
