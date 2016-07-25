using NUnit.Framework;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.Test;
using Peach.Pro.Core.MutationStrategies;

namespace Peach.Pro.Test.Core.MutationStrategies
{
	class WebApiData : ActionData
	{
		
	}

	[TestFixture]
	[Peach]
	[Quick]
	internal class WebProxyTests
	{
		[Test]
		public void TestCreate()
		{
			var ctx = new RunContext
			{
				config = new RunConfiguration(),
				test = new Peach.Core.Dom.Test()
			};

			var strategy = new WebProxyStrategy(null);

			strategy.Initialize(ctx, null);

			strategy.Finalize(ctx, null);
		}
	}
}
