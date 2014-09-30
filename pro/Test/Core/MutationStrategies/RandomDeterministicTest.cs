using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.Analyzers;
using Peach.Core.IO;
using NUnit.Framework;

namespace Peach.Core.Test.MutationStrategies
{
	[TestFixture] [Category("Peach")]
	class RandomDeterministicTest : DataModelCollector
	{
		[Test]
		public void Test1()
		{
			// Runs just like sequential, but jumps around
			// to run a different element/mutator every iteration

			string xml = @"
<Peach>
	<DataModel name='TheDataModel'>
		<String name='str1' value='Hello World!' />
		<String name='str2' value='Hello World!' />
		<String name='str3' value='Hello World!' />
		<String name='str4' value='Hello World!' />
		<String name='str5' value='Hello World!' />
	</DataModel>

	<StateModel name='TheState' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='TheDataModel' />
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='TheState' />
		<Publisher class='Null' />
		<Strategy class='RandomDeterministic' />
	</Test>
</Peach>";

			RunEngine(xml);

			// Expect many mutations
			Assert.Greater(mutations.Count, 11000);

			// this strategy fuzzes elements in a consistently random order
			// that appears to be random.  should not run the same strategy twice
			// for > 85% of the time.
			Assert.Greater(strategies.Count, (mutations.Count * 85) / 100);
		}

	}
}
