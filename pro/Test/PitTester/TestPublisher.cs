using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.IO;
using Peach.Pro.Core.Publishers;

namespace PitTester
{
	public class TestPublisher : StreamPublisher
	{
		static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

		string name;
		TestLogger testLogger;

		public TestPublisher(string name, TestLogger testLogger)
			: base(new Dictionary<string, Variant>())
		{
			this.name = name;
			this.testLogger = testLogger;
			this.stream = new MemoryStream();
		}

		protected override NLog.Logger Logger
		{
			get { return logger; }
		}

		protected override void OnStart()
		{
			//testLogger.Verify<TestData.Start>(name);
		}

		protected override void OnStop()
		{
			//testLogger.Verify<TestData.Stop>(name);
		}

		protected override void OnOpen()
		{
			testLogger.Verify<TestData.Open>(name);
		}

		protected override void OnClose()
		{
			testLogger.Verify<TestData.Close>(name);

			if (stream.Position != stream.Length)
				throw new Exception(string.Format("Error, input stream has {0} unconsumed bytes from last input action.",
					stream.Length - stream.Position));
		}

		protected override void OnAccept()
		{
			testLogger.Verify<TestData.Accept>(name);
		}

		protected override Variant OnCall(string method, List<ActionParameter> args)
		{
			testLogger.Verify<TestData.Call>(name);
			throw new NotSupportedException();
		}

		protected override void OnSetProperty(string property, Variant value)
		{
			testLogger.Verify<TestData.SetProperty>(name);
			//throw new NotSupportedException();
		}

		protected override Variant OnGetProperty(string property)
		{
			testLogger.Verify<TestData.GetProperty>(name);
			throw new NotSupportedException();
		}

		protected override void OnInput()
		{
			var data = testLogger.Verify<TestData.Input>(name);

			if (data.IsDatagram)
			{
				// This is the 'Datagram' publisher behavior
				stream.Seek(0, SeekOrigin.Begin);
				stream.Write(data.Payload, 0, data.Payload.Length);
				stream.SetLength(data.Payload.Length);
				stream.Seek(0, SeekOrigin.Begin);
			}
			else
			{
				if (stream.Position != stream.Length)
					throw new Exception(string.Format("Error, input stream has {0} unconsumed bytes from last input action.",
						stream.Length - stream.Position));

				// This is the 'Stream' publisher behavior
				var pos = stream.Position;
				stream.Seek(0, SeekOrigin.End);
				stream.Write(data.Payload, 0, data.Payload.Length);
				stream.Seek(pos, SeekOrigin.Begin);

				// TODO: For stream publishers, defer putting all of the
				// payload into this.stream and use 'WantBytes' to
				// deliver more bytes
			}
		}

		public override void output(DataModel dataModel)
		{
			var data = testLogger.Verify<TestData.Output>(name);
			var expected = data.Payload;

			// Only check outputs on non-fuzzing iterations
			if (!this.Test.parent.context.controlIteration)
				return;

			// Ensure we end on a byte boundary
			var bs = dataModel.Value.PadBits();
			bs.Seek(0, SeekOrigin.Begin);
			var actual = new BitReader(bs).ReadBytes((int)bs.Length);

			// If this data model has a file data set, compare to that
			var dataSet = dataModel.actionData.selectedData as Peach.Core.Dom.DataFile;
			if (dataSet != null)
				expected = File.ReadAllBytes(dataSet.FileName);

			if (Logger.IsDebugEnabled)
				Logger.Debug("\n\n" + Utilities.HexDump(actual, 0, actual.Length));

			if (expected.Length != actual.Length)
				throw new PeachException("Length mismatch in action {0}. Expected {1} bytes but got {2} bytes.".Fmt(testLogger.ActionName, expected.Length, actual.Length));

			var skipList = new List<Tuple<long, long>>();

			foreach (var ignore in testLogger.Ignores)
			{
				var act = ignore.Item1;
				var elem = ignore.Item2;

				if (act != dataModel.actionData.action)
					continue;

				var tgt = dataModel.find(elem.fullName);
				if (tgt == null)
					throw new PeachException("Error, couldn't locate {0} in model on action {1} for ignoring.".Fmt(elem, testLogger.ActionName));

				long pos;
				var lst = dataModel.Value as BitStreamList;
				if (!lst.TryGetPosition(elem.fullName, out pos))
					throw new PeachException("Error, couldn't locate position of {0} in model on action {1} for ignoring.".Fmt(elem, testLogger.ActionName));

				skipList.Add(new Tuple<long, long>(pos / 8, (pos / 8) + (tgt.Value.LengthBits + 7) / 8));
			}

			if (skipList.Any())
				Logger.Debug("Ignoring bytyes {0}", string.Join(", ", skipList.Select(i => "{0}:{1}".Fmt(i.Item1, i.Item2))));
			for (int i = 0; i < actual.Length; ++i)
			{
				var skip = skipList.Where(p => p.Item1 <= i && p.Item2 > i).Any();
				if (!skip && expected[i] != actual[i])
					throw new PeachException("\nTest failed on action: {0}\n\tValues differ at offset 0x{3:x8}\n\tExpected: 0x{1:x2}\n\tBut was: 0x{2:x2}\n".Fmt(testLogger.ActionName, expected[i], actual[i], i));
			}
		}

		protected override void OnOutput(BitwiseStream data)
		{
			// Handled with the override for output()
			throw new NotSupportedException();
		}
	}
}
