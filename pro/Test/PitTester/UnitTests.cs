using NUnit.Framework;
using Peach.Enterprise.WebServices;
using Peach.Enterprise.WebServices.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PitTester
{
	[TestFixture]
	class UnitTests
	{
		public class TestCase
		{
			public Pit Pit
			{
				get;
				set;
			}

			public override string ToString()
			{
				return Pit.Name;
			}
		}

		static UnitTests()
		{
			LibraryPath = "../../../pits/pro".Replace('/', Path.DirectorySeparatorChar);

			var lib = new PitDatabase(LibraryPath);
			var errors = new StringBuilder();

			lib.ValidationEventHandler += delegate(object sender, ValidationEventArgs e)
			{
				errors.AppendLine(e.FileName);
				errors.AppendLine(e.Exception.Message);
				errors.AppendLine();
			};

			// Ignore user pits
			AllPits = lib.Entries.Where(e => e.Locked).Select(p => new TestCase() { Pit = p }).ToList();

			LoadErrors = errors.ToString();
		}

		public static string LibraryPath
		{
			get;
			private set;
		}

		public static IEnumerable<TestCase> AllPits
		{
			get;
			private set;
		}

		public static string LoadErrors
		{
			get;
			private set;
		}

		[Test]
		public void LoadLibrary()
		{
			if (!string.IsNullOrEmpty(LoadErrors))
				Assert.Fail(LoadErrors);

			Assert.Greater(AllPits.Count(), 0);
		}

		[Test]
		public void VerifyConfig([ValueSource("AllPits")]TestCase test)
		{
			var errors = new StringBuilder();
			var fileName = test.Pit.Versions[0].Files[0].Name;

			try
			{
				PitTester.VerifyPitConfig(fileName);
			}
			catch (Exception ex)
			{
				errors.AppendFormat("{0}.config", fileName);
				errors.AppendLine();
				errors.AppendLine(ex.Message);
			}

			if (errors.Length > 0)
				Assert.Fail(errors.ToString());
		}

		[Test]
		public void Verify([ValueSource("AllPits")]TestCase test)
		{
			var errors = new StringBuilder();

			for (int i = 0; i < test.Pit.Versions[0].Files.Count; ++i)
			{
				var fileName = test.Pit.Versions[0].Files[i].Name;

				try
				{
					PitTester.VerifyPit(LibraryPath, fileName, i == 0);
				}
				catch (Exception ex)
				{
					errors.AppendFormat("{0}", fileName);
					errors.AppendLine();
					errors.AppendLine(ex.Message);
				}
			}

			if (errors.Length > 0)
				Assert.Fail(errors.ToString());
		}

		[Test]
		public void Run([ValueSource("AllPits")]TestCase test)
		{
			var fileName = test.Pit.Versions[0].Files[0].Name;

			try
			{
				PitTester.TestPit(LibraryPath, fileName, false, null);
			}
			catch (FileNotFoundException)
			{
				Assert.Ignore("No test definition found.");
			}
			catch (Exception ex)
			{
				Assert.Fail(ex.Message);
			}
		}
	}
}
