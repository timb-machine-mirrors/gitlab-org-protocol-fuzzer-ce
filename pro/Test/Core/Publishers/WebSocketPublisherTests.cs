using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using Peach.Core;
using Peach.Core.Dom;
using System.IO;
using Peach.Core.Analyzers;

namespace Peach.Core.Test.Publishers
{
	[TestFixture]
	[Category("Peach")]
	class WebSocketPublisherTests
	{
		[Test]
		public void TestCreate()
		{
			var tmp = Path.GetTempFileName();

			string xml = @"
<Peach>
	<DataModel name='DM'>
		<String value='Hello World' />
	</DataModel>

	<StateModel name='SM' initialState='Initial'>
		<State name='Initial'>
			<!-- No actions, just want to construct/desctuct the publisher -->
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='SM' />
		<Publisher class='WebSocket'>
			<Param name='Template' value='{0}' />
		</Publisher>
	</Test>
</Peach>
";

			var template =
@"<html>
<body>
</body>
</html>";

			File.WriteAllBytes(tmp, Encoding.ASCII.GetBytes(template));
			xml = xml.Fmt(tmp);

			try
			{
				var dom = DataModelCollector.ParsePit(xml);

				var config = new RunConfiguration()
				{
					singleIteration = true,
				};

				var e = new Engine(null);
				e.startFuzzing(dom, config);
			}
			finally
			{
				File.Delete(tmp);
			}
		}
	}
}
