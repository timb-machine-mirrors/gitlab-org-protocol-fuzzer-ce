using System;
using System.Linq;
using Peach.Core;
using NUnit.Framework;

namespace Peach.Core.Test.Mutators
{
	[TestFixture]
	class SizedVarianceTests : DataModelCollector
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
			<Mutator class='SizedVariance' />
		</Mutators>
	</Test>
</Peach>
";

			RunEngine(xml);

			// Size is 11 bytes, max is 50
			// (50 - 4) = 46 expansions
			Assert.AreEqual(46, mutatedDataModels.Count);
		}
	}
}
