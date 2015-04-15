using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Peach.Core;
using Peach.Pro.Core.Storage;

namespace Peach.Pro.Test.Core.Storage
{
	class DatabaseTests
	{
		public static void AssertResult<T>(IEnumerable<T> actual, IEnumerable<T> expected)
		{
			var actualList = actual.ToList();
			var expectedList = expected.ToList();

			Database.Dump(actualList);

			Assert.AreEqual(expectedList.Count, actualList.Count, "Rows mismatch");

			var type = typeof(T);
			for (var i = 0; i < actualList.Count; i++)
			{
				var actualRow = actualList[i];
				var expectedRow = expectedList[i];
				foreach (var pi in type.GetProperties()
					.Where(x => !x.HasAttribute<NotMappedAttribute>()))
				{
					var actualValue = pi.GetValue(actualRow, null);
					var expectedValue = pi.GetValue(expectedRow, null);
					var msg = "Values mismatch on row {0} column {1}.".Fmt(i, pi.Name);
					Assert.AreEqual(expectedValue, actualValue, msg);
				}
			}
		}
	}
}
