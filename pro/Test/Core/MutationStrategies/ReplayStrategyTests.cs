using NUnit.Framework;
using Peach.Core.Test;

namespace Peach.Pro.Test.Core.MutationStrategies
{
	[TestFixture]
	[Quick]
	[Peach]
	class ReplayStrategyTests
	{
		public void Test1()
		{
			Assert.AreEqual(0, 0);
		}
	}
}
