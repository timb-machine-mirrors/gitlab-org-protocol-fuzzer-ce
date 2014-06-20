using System;
using System.Collections.Generic;
using Peach.Enterprise.WebServices.Models;

namespace Peach.Enterprise.WebServices
{
	public class PitTester
	{
		private PitTester()
		{
			Guid = System.Guid.NewGuid().ToString();
		}

		public static PitTester Run(string pitLibraryPath, string pitFile)
		{
			return new PitTester();
		}

		public string Guid
		{
			get;
			private set;
		}

		public TestStatus Status
		{
			get
			{
				return TestStatus.Pass;
			}
		}

		public TestResult Result
		{
			get
			{
				return new TestResult()
				{
					Status = TestStatus.Pass,
					Events = new List<TestEvent>(new[]
					{
						new TestEvent()
						{
							Id = 0,
							Status = TestStatus.Pass,
							Short = "Starting Engine",
							Description = "The description of the engine starting",
							Resolve = "Resolution instructions"
						},
						new TestEvent()
						{
							Id = 1,
							Status = TestStatus.Pass,
							Short = "Finished",
							Description = "Finished description",
							Resolve = "How to resolve"
						},
					})
				};
			}
		}

		public string Log
		{
			get
			{
				return "The log from the test goes here!";
			}
		}
	}
}
