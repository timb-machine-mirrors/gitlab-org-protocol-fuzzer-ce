using System;

using NUnit.Framework;

namespace Peach.Core.Test.Mutators
{
	[TestFixture]
	class StringAsciiRandomTests : StringMutatorTester
	{
		public StringAsciiRandomTests()
			: base("StringAsciiRandom")
		{
			// Verify fuzzed string lengths for sequential
			VerifyLength = true;
		}

		[Test]
		public void TestSupported()
		{
			RunSupported();
		}

		[Test]
		public void TestSequential()
		{
			RunSequential();
		}

		[Test]
		public void TestRandom()
		{
			RunRandom();
		}

		[Test]
		public void TestMaxOutputSize()
		{
			string xml = @"
<Peach>
	<DataModel name='DM'>
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
			<Mutator class='StringAsciiRandom' />
		</Mutators>
	</Test>
</Peach>
";

			RunEngine(xml);

			// min is 0, max is 50 (skip 11) = 50 mutations
			Assert.AreEqual(50, mutatedDataModels.Count);

			foreach (var m in mutatedDataModels)
				Assert.LessOrEqual(m.Value.Length, 50);
		}
	}
}
