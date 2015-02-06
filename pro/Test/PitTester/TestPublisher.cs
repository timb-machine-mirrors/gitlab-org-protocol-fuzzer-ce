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
		static readonly NLog.Logger ClassLogger = NLog.LogManager.GetCurrentClassLogger();

		bool _datagram;
		readonly TestLogger _logger;

		public TestPublisher(TestLogger logger)
			: base(new Dictionary<string, Variant>())
		{
			_logger = logger;
			stream = new MemoryStream();
		}

		protected override NLog.Logger Logger
		{
			get { return ClassLogger; }
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
			_logger.Verify<TestData.Open>(Name);
		}

		protected override void OnClose()
		{
			_logger.Verify<TestData.Close>(Name);

			// Don't verify stream positions if previous error occurred
			if (_logger.ExceptionOccurred)
				return;

			if (!_datagram && stream.Position != stream.Length)
				throw new Exception(string.Format("Error, input stream has {0} unconsumed bytes from last input action.",
					stream.Length - stream.Position));
		}

		protected override void OnAccept()
		{
			_logger.Verify<TestData.Accept>(Name);
		}

		public override Variant call(string method, List<ActionParameter> args)
		{
			throw new NotImplementedException();
		}

		protected override Variant OnCall(string method, List<BitwiseStream> args)
		{
			// Handled with the override for output()
			throw new NotSupportedException();
		}

		protected override void OnSetProperty(string property, Variant value)
		{
			_logger.Verify<TestData.SetProperty>(Name);
		}

		protected override Variant OnGetProperty(string property)
		{
			_logger.Verify<TestData.GetProperty>(Name);

			throw new NotImplementedException();
		}

		protected override void OnInput()
		{
			var data = _logger.Verify<TestData.Input>(Name);

			_datagram = data.IsDatagram;

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
			var data = _logger.Verify<TestData.Output>(Name);
			var expected = data.Payload;

			// Only check outputs on non-fuzzing iterations
			if (!IsControlIteration)
				return;

			// Ensure we end on a byte boundary
			var bs = dataModel.Value.PadBits();
			bs.Seek(0, SeekOrigin.Begin);
			var actual = new BitReader(bs).ReadBytes((int)bs.Length);

			// If this data model has a file data set, compare to that
			var dataSet = dataModel.actionData.selectedData as DataFile;
			if (dataSet != null)
				expected = File.ReadAllBytes(dataSet.FileName);

			if (Logger.IsDebugEnabled)
				Logger.Debug("\n\n" + Utilities.HexDump(actual, 0, actual.Length));

		    var skipList = new List<Tuple<string, long, long>>();

			foreach (var ignore in _logger.Ignores)
			{
				var act = ignore.Item1;
				var elem = ignore.Item2;

				if (act != dataModel.actionData.action)
					continue;

				var tgt = dataModel.find(elem.fullName);
				if (tgt == null)
				{
					// Can happen when we ignore non-selected choice elements
					Logger.Debug("Couldn't locate {0} in model on action {1} for ignoring.", elem, _logger.ActionName);
					continue;
				}

				// If we found the data element in the model, we expect to find its position

				long pos;
				var lst = (BitStreamList)dataModel.Value;
				if (!lst.TryGetPosition(elem.fullName, out pos))
					throw new PeachException("Error, Couldn't locate position of {0} in model on action {1} for ignoring.".Fmt(elem, _logger.ActionName));

				skipList.Add(new Tuple<string, long, long>(elem.fullName, pos / 8, (pos / 8) + (tgt.Value.LengthBits + 7) / 8));
			}

			foreach (var i in skipList)
				Logger.Debug("Ignoring {0} from index {1} to {2}", i.Item1, i.Item2, i.Item3);

		    var checkLength = actual.Length > expected.Length ? expected.Length : actual.Length;
            for (var i = 0; i < checkLength; ++i)
		    {
		        var skip = skipList.Any(p => p.Item2 <= i && p.Item3 > i);
		        if (skip || expected[i] == actual[i]) continue;

		        if (Logger.IsDebugEnabled)
		        {
		            Logger.Debug("vv Dumping DataModel vvvvvvvvvvvvvvvvv");
		            long pos = 0;

		            foreach (var item in dataModel.Walk())
		            {
		                Logger.Debug("0x{1:x}-0x{2:X}: {0}: {3}", 
		                    item.fullName, 
		                    pos,
		                    pos + item.Value.Length,
		                    Utilities.HexDump(item.Value));

		                if(!(item is DataElementContainer))
		                    pos += item.Value.Length;
		            }
		            Logger.Debug("^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^");
                }

		        throw new PeachException(
		            "\nTest failed on action: {0}\n\tValues differ at offset 0x{3:x8}\n\tExpected: 0x{1:x2}\n\tBut was: 0x{2:x2}\n"
						.Fmt(_logger.ActionName, expected[i], actual[i], i));
		    }

            // Perform length check after byte comparison. More usefull this way.
            if (expected.Length != actual.Length)
            {
                if (Logger.IsDebugEnabled)
                {
                    Logger.Debug("vv Dumping DataModel vvvvvvvvvvvvvvvvv");
                    long pos = 0;

                    foreach (var item in dataModel.Walk())
                    {
                        Logger.Debug("0x{1:x}-0x{2:X}: {0}: {3}",
                            item.fullName,
                            pos,
                            pos + item.Value.Length,
                            Utilities.HexDump(item.Value));

                        if (!(item is DataElementContainer))
                            pos += item.Value.Length;
                    }
                    Logger.Debug("^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^");
                }

                throw new PeachException(
					"Length mismatch in action {0}. Expected {1} bytes but got {2} bytes.".Fmt(_logger.ActionName,
                        expected.Length, actual.Length));
            }

        }

		protected override void OnOutput(BitwiseStream data)
		{
			// Handled with the override for output()
			throw new NotSupportedException();
		}
	}
}
