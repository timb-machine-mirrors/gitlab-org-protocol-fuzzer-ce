using NUnit.Framework;
using Peach.Core.Dom;
using Peach.Core.Mutators.Utility;
using System;
using System.Text;

namespace Peach.Core.Test.Mutators.Utility
{
	[TestFixture]
	class IntegerEdgeCasesTests
	{
		class Tester : IntegerEdgeCases
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
				: base(obj)
			{
				this.context = new Strategy();
			}

			static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

			protected override NLog.Logger Logger
			{
				get { return logger; }
			}

			protected override void GetLimits(DataElement obj, out long min, out ulong max)
			{
				var num = (Number)obj;

				min = num.MinValue;
				max = num.MaxValue;
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

		public void TestSequential(int size, bool signed, int min, int count)
		{
			var num = new Number("num") { length = size, Signed = signed };

			var tester = new Tester(num);

			Assert.AreEqual(count, tester.count);

			uint i = 0;

			tester.LongMutation = v => Assert.AreEqual(min + i, v);
			tester.ULongMutation = v => Assert.Fail();

			for (i = 0; i < tester.count; ++i)
			{
				tester.mutation = i;
				tester.sequentialMutation(null);
			}
		}

		[Test]
		public void TestSequential()
		{
			// For numbers <= 8 bits, sequential should just hit every number

			TestSequential(4, false, 0, 16);
			TestSequential(4, true, -8, 16);
			TestSequential(8, false, 0, 256);
			TestSequential(8, true, -128, 256);
		}

		public void TestRandom(int size, bool signed, int min, int count)
		{
			var num = new Number("num") { length = size, Signed = signed };

			var tester = new Tester(num);

			Assert.AreEqual(count, tester.count);

			uint i = 0;

			int[] totals = new int[count];
			int different = 0;

			tester.LongMutation = v => { ++totals[v - min]; if (v != min + i) ++different; };
			tester.ULongMutation = v => Assert.Fail();

			for (i = 1; i < 10 * tester.count; ++i)
			{
				tester.context.Iteration = i;
				tester.randomMutation(null);
			}

			// Ensure that for more than half of the mutations,
			// that the iteration number is not the same as the
			// number used for mutation
			Assert.GreaterOrEqual(different, count / 2);

			var sb = new StringBuilder();

			for (i = 0; i < totals.Length; ++i)
				if (totals[i] == 0)
					sb.AppendFormat("{0} ", min + i);

			// Make sure after 10x the number space, we hit all the
			// possible number values.
			var str = sb.ToString();
			if (!string.IsNullOrEmpty(str))
				Assert.Fail("Missed numbers: {0}", str);
		}

		[Test]
		public void TestRandom()
		{
			// For numbers <= 8 bits, random just picks a number in the space

			TestRandom(4, false, 0, 16);
			TestRandom(4, true, -8, 16);
			TestRandom(8, false, 0, 256);
			TestRandom(8, true, -128, 256);
		}
	}
}
