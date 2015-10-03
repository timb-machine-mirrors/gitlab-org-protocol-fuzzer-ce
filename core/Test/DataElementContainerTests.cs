using System.Linq;
using NUnit.Framework;
using Peach.Core.Dom;
using Peach.Core.Test;

namespace Peach.Core.Test
{
	[TestFixture]
	[Quick]
	internal class DataElementContainerTests : DataModelCollector
	{
		[Test]
		[Category("Peach")]
		public void RemoteCleanup()
		{
			var pit = @"<?xml version='1.0' encoding='utf-8'?>
<Peach>
	<DataModel name='Example1'>
		<Block name='Test'>
			<Number name='Of' size='16'>
				<Relation type='size' of='Data'/>
			</Number>
			<Blob name='Data' />
		</Block>
	</DataModel>

	<StateModel name='TheStateModel' initialState='initial'>
		<State name='initial'>
		  <Action name='A1' type='output'>
			<DataModel name='foo'><String value='1' /></DataModel>
		  </Action>
		</State>
	</StateModel>

	<Test name='Default' maxOutputSize='200'>
		<StateModel ref='TheStateModel'/>
		<Publisher class='Null'/>
	</Test>
</Peach>
";
			var dom = ParsePit(pit);
			var testElement = dom.dataModels[0][0];
			var ofElement = dom.dataModels[0].find("Of");

			Assert.AreEqual(1, ofElement.relations.Count);

			dom.dataModels[0].Remove(testElement, false);

			Assert.AreEqual(1, ofElement.relations.Count);

			dom = ParsePit(pit);
			testElement = dom.dataModels[0][0];
			ofElement = dom.dataModels[0].find("Of");

			Assert.AreEqual(1, ofElement.relations.Count);

			dom.dataModels[0].Remove(testElement, true);

			Assert.AreEqual(0, ofElement.relations.Count);
		}
	}
}
