using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using PeachFarm.Common.Messages;
using PeachFarm.Reporting;
using PeachFarm.Common.Mongo;

namespace PeachFarm.Test
{
	class TestHeartbeat : Heartbeat
	{
		public bool __test_was_removed_from_database = false;
		public bool __test_was_saved_to_database = false;
		public bool __test_was_saved_to_errors = false;

		public static bool __testing_prints = false;

		public TestHeartbeat()
			: base()
		{
		}

		#region overrides
		public override void RemoveFromDatabase(String conString)
		{
			if (__testing_prints) System.Console.WriteLine("remove from db");
			__test_was_removed_from_database = true;
		}

		public override Heartbeat SaveToErrors(String conString)
		{
			if (__testing_prints) System.Console.WriteLine("save to errors");
			__test_was_saved_to_errors = true;
			return (Heartbeat)this;
		}

		public override Heartbeat SaveToDatabase(String conString)
		{
			if (__testing_prints) System.Console.WriteLine("save to db");
			__test_was_saved_to_database = true;
			return (Heartbeat)this;
		}
		#endregion
	}
}