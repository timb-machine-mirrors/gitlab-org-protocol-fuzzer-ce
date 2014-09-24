using NUnit.Framework;
using Peach.Core.Dom;
using Peach.Core.Mutators.Utility;
using System;
using System.Collections.Generic;
using System.Text;

namespace Peach.Core.Test.Mutators.Utility
{
	[TestFixture]
	class IntegerVarianceTests
	{
		class Tester : IntegerVariance
		{
			class Strategy : MutationStrategy
			{
				uint iteration;

				public Strategy()
					: base(null)
				{
					_context = new RunContext() { config = new RunConfiguration() };

				}
				public override bool UsesRandomSeed
				{
					get { return false; }
				}

				public override bool IsDeterministic
				{
					get { return false; }
				}

				public override uint Count
				{
					get { return 0; }
				}

				public override uint Iteration
				{
					get
					{
						return iteration;
					}
					set
					{
						iteration = value;
						SeedRandom();
					}
				}
			}

			public Action<long> LongMutation;
			public Action<ulong> ULongMutation;

			public Tester(Number obj)
				: base(obj, false)
			{
				this.context = new Strategy();
			}

			static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

			protected override NLog.Logger Logger
			{
				get { return logger; }
			}

			protected override void GetLimits(DataElement obj, out bool signed, out long value, out long min, out long max)
			{
				var num = (Number)obj;

				signed = num.Signed;
				value = (long)num.DefaultValue;
				min = (long)num.MinValue;
				max = (long)num.MaxValue;
			}

			protected override void performMutation(Dom.DataElement obj, long value)
			{
				LongMutation(value);
			}

			protected override void performMutation(Dom.DataElement obj, ulong value)
			{
				ULongMutation(value);
			}
		}

		void TestVariance(int size, bool signed, long value, int count)
		{
			var num = new Number("num") { length = size, Signed = signed, DefaultValue = new Variant(value) };

			var tester = new Tester(num);

			var totals = new Dictionary<long, int>();

			for (long x = num.MinValue; x <= (long)num.MaxValue; ++x)
				totals[x] = 0;

			tester.LongMutation = v =>
			{
				totals[v] = totals[v] + 1;
			};

			tester.ULongMutation = v => Assert.Fail();

			// Run 10 times the count
			for (uint i = 1; i < count; ++i)
			{
				tester.context.Iteration = i;
				tester.randomMutation(null);
			}

			var sb = new StringBuilder();

			foreach (var kv in totals)
				if (kv.Value == 0 && kv.Key != value)
					sb.AppendFormat("{0} ", kv.Key);

			// Make sure after 10x the number space, we hit all the
			// possible number values.
			var str = sb.ToString();
			if (!string.IsNullOrEmpty(str))
				Assert.Fail("Missed numbers: {0}", str);

			Assert.AreEqual(0, totals[value]);
		}

		[Test]
		public void Test()
		{
			TestVariance(8, true, 0, 100000);
		}
	}
}