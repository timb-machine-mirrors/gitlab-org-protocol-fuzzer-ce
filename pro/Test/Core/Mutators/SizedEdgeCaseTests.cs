using System;
using System.Linq;
using Peach.Core;
using NUnit.Framework;

namespace Peach.Core.Test.Mutators
{
	[TestFixture]
	class SizedEdgeCaseTests : DataModelCollector
	{
		[Test]
		public void TestMaxOutputSize()
		{
			string xml = @"
<Peach>
	<DataModel name='DM'>
		<Number name='num' size='32'>
			<Relation type='size' of='str' />
		</Number>
		<String name='str' value='Hello World' />
	</DataModel>

	<StateModel name='StateModel' initialState='initial'>
		<State name='initial'>
			<Action type='output'>
				<DataModel ref='DM'/>
			</Action> 
		</State>
	</StateModel>

	<Test name='Default' maxOutputSize='50'>
		<StateModel ref='StateModel'/>
		<Publisher class='Null'/>
		<Strategy class='Sequential'/>
		<Mutators mode='include'>
			<Mutator class='SizedEdgeCase' />
		</Mutators>
	</Test>
</Peach>
";

			RunEngine(xml);

			// Size is 11 bytes, max is 50
			// (50 - 4) + 1 = 47 expansions
			Assert.AreEqual(47, mutatedDataModels.Count);
		}
	}
}
