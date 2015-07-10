
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NLog;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.IO;
using Peach.Core.Publishers;
using Logger = NLog.Logger;
using System.Text;
using String = System.String;

namespace PitTester
{
	public class TestPublisher : StreamPublisher
	{
		static readonly Logger ClassLogger = LogManager.GetCurrentClassLogger();

		bool _datagram;
		readonly TestLogger _logger;

		public TestPublisher(TestLogger logger)
			: base(new Dictionary<string, Variant>())
		{
			_logger = logger;
			stream = new MemoryStream();
		}

		protected override Logger Logger
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

		protected override Variant OnCall(string method, List<ActionParameter> args)
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

			if (Logger.IsDebugEnabled)
				Logger.Debug("\n\n" + Utilities.HexDump(data.Payload, 0, data.Payload.Length));

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

		    var skipList = new List<Tuple<long, long>>();

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
					Logger.Debug("Couldn't locate {0} in model on action {1} for ignoring.", elem.debugName, _logger.ActionName);
					continue;
				}

				// If we found the data element in the model, we expect to find its position

				long pos;
				var lst = (BitStreamList)dataModel.Value;
				if (!lst.TryGetPosition(elem.fullName, out pos))
					throw new PeachException("Error, Couldn't locate position of {0} in model on action {1} for ignoring.".Fmt(elem, _logger.ActionName));

				var skip = new Tuple<long, long>(pos / 8, (pos / 8) + (tgt.Value.LengthBits + 7) / 8);
				Logger.Debug("Ignoring {0} from index {1} to {2}", elem.fullName, skip.Item1, skip.Item2);
				skipList.Add(skip);
			}

			var cb = new ConsoleBuffer();
			if (BinDiff(expected, actual, skipList, cb))
			{
				cb.Print();
				throw new PeachException("Values differ on action: {0}".Fmt(_logger.ActionName));
			}
        }

		protected override void OnOutput(BitwiseStream data)
		{
			// Handled with the override for output()
			throw new NotSupportedException();
		}

		bool BinDiff(byte[] expected, byte[] actual, List<Tuple<long,long>> skipList, ConsoleBuffer cb)
		{
			var ms1 = new MemoryStream(expected);
			var ms2 = new MemoryStream(actual);
			
			const int bytesPerLine = 16;

			var diff = false;
			var bytes1 = new byte[bytesPerLine];
			var bytes2 = new byte[bytesPerLine];

			for (var i = 0;; i += bytesPerLine)
			{
				var readLen1 = ms1.Read(bytes1, 0, bytesPerLine);
				var readLen2 = ms2.Read(bytes2, 0, bytesPerLine);
				if (readLen1 == 0 && readLen2 == 0)
					break;

				var hex1 = new ConsoleBuffer();
				var hex2 = new ConsoleBuffer();

				var ascii1 = new ConsoleBuffer();
				var ascii2 = new ConsoleBuffer();

				var lineDiff = false;
				for (var j = 0; j < bytesPerLine; j++)
				{
					ConsoleColor bg;
					var offset = i + j;
					var skip = skipList.Any(p => p.Item1 <= offset && p.Item2 > offset);
					if (skip)
					{
						bg = ConsoleColor.Blue;
					}
					else
					{
						bg = ConsoleColor.Black;
					}

					if (j < readLen1 && j < readLen2)
					{
						if (bytes1[j] == bytes2[j])
						{
							hex1.Append(ConsoleColor.Gray, bg, "{0:X2}".Fmt(bytes1[j]));
							hex1.Append(" ");
							ascii1.Append(ConsoleColor.Gray, bg, "{0}".Fmt(ByteToAscii(bytes1[j])));

							hex2.Append(ConsoleColor.Gray, bg, "{0:X2}".Fmt(bytes2[j]));
							hex2.Append(" ");
							ascii2.Append(ConsoleColor.Gray, bg, "{0}".Fmt(ByteToAscii(bytes2[j])));
						}
						else
						{
							if (!skip) lineDiff = true;

							hex1.Append(ConsoleColor.Green, bg, "{0:X2}".Fmt(bytes1[j]));
							hex1.Append(" ");
							ascii1.Append(ConsoleColor.Green, bg, "{0}".Fmt(ByteToAscii(bytes1[j])));

							hex2.Append(ConsoleColor.Red, bg, "{0:X2}".Fmt(bytes2[j]));
							hex2.Append(" ");
							ascii2.Append(ConsoleColor.Red, bg, "{0}".Fmt(ByteToAscii(bytes2[j])));
						}
					}
					else if (j < readLen1)
					{
						if (!skip) lineDiff = true;

						hex1.Append(ConsoleColor.Green, bg, "{0:X2}".Fmt(bytes1[j]));
						hex1.Append(" ");
						ascii1.Append(ConsoleColor.Green, bg, "{0}".Fmt(ByteToAscii(bytes1[j])));

						hex2.Append("   ");
						ascii2.Append(" ");
					}
					else if (j < readLen2)
					{
						if (!skip) lineDiff = true;

						hex1.Append("   ");
						ascii1.Append(" ");

						hex2.Append(ConsoleColor.Red, bg, "{0:X2}".Fmt(bytes2[j]));
						hex2.Append(" ");
						ascii2.Append(ConsoleColor.Red, bg, "{0}".Fmt(ByteToAscii(bytes2[j])));
					}
					else
					{
						hex1.Append("   ");
						ascii1.Append(" ");

						hex2.Append("   ");
						ascii2.Append(" ");
					}
				}

				cb.Append("{0:X8}   ".Fmt(i));
				cb.Append(hex1);
				cb.Append("  ");
				cb.Append(ascii1);
				cb.Append(Environment.NewLine);

				if (lineDiff)
				{
					diff = true;

					cb.Append("           ");
					cb.Append(hex2);
					cb.Append("  ");
					cb.Append(ascii2);
					cb.Append(Environment.NewLine);
				}
			}

			return diff;
		}

		char ByteToAscii(byte b)
		{
			return ((b < 32 || b > 126) ? '.' : (char)b);
		}
	}

	class ConsoleRegion
	{
		public ConsoleColor ForegroundColor { get; set; }
		public ConsoleColor BackgroundColor { get; set; }
		public string String { get; set; }
	}

	class ConsoleBuffer
	{
		List<ConsoleRegion> _regions = new List<ConsoleRegion>();

		public void Append(ConsoleColor fg, ConsoleColor bg, string str)
		{
			_regions.Add(new ConsoleRegion {
				ForegroundColor = fg,
				BackgroundColor = bg,
				String = str,
			});
		}

		public void Append(string str)
		{
			Append(ConsoleColor.Gray, ConsoleColor.Black, str);
		}

		public void Append(ConsoleBuffer cb)
		{
			_regions.AddRange(cb._regions);
		}

		public void Print()
		{
			var fg = Console.ForegroundColor;
			var bg = Console.BackgroundColor;
			try
			{
				foreach (var region in _regions)
				{
					Console.ForegroundColor = region.ForegroundColor;
					Console.BackgroundColor = region.BackgroundColor;
					Console.Write(region.String);
				}
			}
			finally
			{
				Console.ForegroundColor = fg;
				Console.BackgroundColor = bg;
			}
		}
	}
}
