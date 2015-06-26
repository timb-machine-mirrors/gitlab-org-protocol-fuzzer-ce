using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Test;
using Peach.Pro.Core.Storage;

namespace Peach.Pro.Test.Core.Storage
{
	[TestFixture]
	[Quick]
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

		class TestTable 
		{			
			[Key]
			public long Id { get; set; }

			public string Value { get; set; }
		}

		class TestDatabase : Database
		{
			public TestDatabase(string path)
				: base(path, false)
			{
			}

			protected override IEnumerable<Type> Schema
			{ 
				get { return new[] { typeof(TestTable) }; }
			}
			
			protected override IEnumerable<string> Scripts { get { return null; } }

			protected override IList<MigrationHandler> Migrations
			{
				get { return TestMigrations; }
			}

			public List<MigrationHandler> TestMigrations = new List<MigrationHandler>();
		}

		TempDirectory _tmp;

		[SetUp]
		public void SetUp()
		{
			_tmp = new TempDirectory();
		}

		[TearDown]
		public void TearDown()
		{
			_tmp.Dispose();
		}


		[Test]
		public void Migration()
		{
			var path = Path.Combine(_tmp.Path, "test.db");
			var builder = new SqliteConnectionBuilder 
			{
				DataSource = path,
				ForeignKeys = true,
				UseWAL = false,
			};

			// Create Version 0
			using (var cnn = builder.Create())
			{
				cnn.Open();
			}

			var history = new List<string>();

			// Update to current (version 2)
			using (var db = new TestDatabase(path))
			{
				Assert.AreEqual(0, db.CurrentVersion);

				db.TestMigrations.Add(() => history.Add("1"));
				db.TestMigrations.Add(() => history.Add("2"));

				db.Migrate();

				Assert.AreEqual(2, db.CurrentVersion);
			}

			var expected = new[]
			{
				"1",
				"2",
			};

			Assert.That(history.ToArray(), Is.EqualTo(expected));

			// Add version 3 & 4
			history.Clear();

			using (var db = new TestDatabase(path))
			{
				Assert.AreEqual(2, db.CurrentVersion);

				db.TestMigrations.Add(() => history.Add("1"));
				db.TestMigrations.Add(() => history.Add("2"));
				db.TestMigrations.Add(() => history.Add("3"));
				db.TestMigrations.Add(() => history.Add("4"));

				db.Migrate();

				Assert.AreEqual(4, db.CurrentVersion);
			}

			expected = new[]
			{
				"3",
				"4",
			};

			Assert.That(history.ToArray(), Is.EqualTo(expected));
		}
	}
}
