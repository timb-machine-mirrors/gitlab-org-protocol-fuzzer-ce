using System;
using System.Collections.Generic;
using Org.BouncyCastle.Crypto.Tls;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.IO;
using Peach.Pro.Core.Fixups;
using DescriptionAttribute = System.ComponentModel.DescriptionAttribute;

namespace Peach.Pro.Core.Transformers.Crypto
{
	public enum TlsAction
	{
		Both,
		Encrypt,
		Decrypt
	}

	[Description("Tls Transformer")]
	[Transformer("Tls", true, Internal = true)]
	[Parameter("ContentType", typeof(byte), "Type of message to encrypt/decrypt")]
	[Parameter("IsServer", typeof(bool), "Is server?", "false")]
	[Parameter("Action", typeof(TlsAction), "Action to perform [Both, Encrypt, Decrypt]", "Both")]
	[Parameter("TlsVersion", typeof(TlsVersion), "TLS Version", "TLSv10")]
	[Serializable]
	public class Tls : Transformer
	{
		BitStream _encodedData;
		DataModel _dataModel;

		public TlsAction Action { get; set; }
		public bool IsServer { get; set; }
		public byte ContentType { get; set; }
		public TlsVersion TlsVersion { get; set; }
		public ProtocolVersion ProtocolVersion
		{
			get
			{
				switch (TlsVersion)
				{
					case TlsVersion.TLSv10:
						return ProtocolVersion.TLSv10;
					case TlsVersion.TLSv11:
						return ProtocolVersion.TLSv11;
					case TlsVersion.TLSv12:
						return ProtocolVersion.TLSv12;
					case TlsVersion.DTLSv10:
						return ProtocolVersion.DTLSv10;
					case TlsVersion.DTLSv12:
						return ProtocolVersion.DTLSv12;
					default:
						throw new PeachException("TlsFixup does not yet support selected TlsVersion.");
				}
			}
		}

		public Tls(DataElement parent, Dictionary<string, Variant> args)
			: base(parent, args)
		{
			ParameterParser.Parse(this, args);
		}

		TlsBlockCipher GetBlockCipher()
		{
			if (_dataModel == null)
			{
				_dataModel = parent.getRoot() as DataModel;
				_dataModel.ActionRun += OnActionRunEvent;
			}

			// We are not running inside an action!
			if (_dataModel.actionData == null)
				return null;

			var ctx = _dataModel.actionData.action.parent.parent.parent.context;

			// The Tls fixup needs to have run first or this transformer is useless
			object obj;
			ctx.iterationStateStore.TryGetValue("TlsBlockCipher", out obj);
			return (TlsBlockCipher)obj;
		}

		static T GetState<T>(RunContext ctx, string key) where T : class
		{
			object obj;
			if (ctx.iterationStateStore.TryGetValue(key, out obj))
				return (T)obj;
			return default(T);
		}

		// Called every time an output action occurs
		void OnActionRunEvent(RunContext ctx)
		{
			parent.Invalidate();

			object sequenceCounter;
			if (ctx.iterationStateStore.TryGetValue("TlsSequenceCounter", out sequenceCounter))
				ctx.iterationStateStore["TlsSequenceCounter"] = ((Int32)sequenceCounter) + 1;
			else
				ctx.iterationStateStore["TlsSequenceCounter"] = 0;
		}

		long GetSequenceCounter(RunContext ctx)
		{
			if (TlsVersion == TlsVersion.DTLSv10 || TlsVersion == TlsVersion.DTLSv12)
			{
				var def = new Number() {DefaultValue = new Variant(0)};
				var epoc = _dataModel.find("Epoc") ?? def;
				var seq = _dataModel.find("SequenceNumber") ?? def;

				var bitstream = new BitStream();
				var writer = new BitWriter(bitstream);
				var reader = new BitReader(bitstream);

				writer.WriteBits((ulong)epoc.DefaultValue, 16);
				writer.WriteBits((ulong) seq.DefaultValue, 48);

				bitstream.Position = 0;

				return reader.ReadInt64();
			}

			object sequenceCounter;
			if (ctx.iterationStateStore.TryGetValue("TlsSequenceCounter", out sequenceCounter))
				return (Int32)sequenceCounter;

			return 0;
		}

		[OnCloned]
		void OnCloned(Tls original, object context)
		{
			// The event is not serialized so we need to
			// resubscribe when we are cloned.
			if (_dataModel != null)
				_dataModel.ActionRun += OnActionRunEvent;
		}

		protected TlsBlockCipher CreateBlockCipher(RunContext ctx)
		{
			try
			{
				var tlsContext = TlsFixup.CreateTlsContext(ctx, IsServer, ProtocolVersion);
				return TlsFixup.CreateBlockCipher(ctx, tlsContext, false);
			}
			catch (Exception)
			{
				return null;
			}
		}

		protected override BitStream internalDecode(BitStream data)
		{
			if (Action == TlsAction.Encrypt)
				return data;

			// Get the cipher.  If it is null, this means we are not in a running action
			var cipher = GetBlockCipher();
			if (cipher == null)
			{
				cipher = CreateBlockCipher(_dataModel.actionData.action.parent.parent.parent.context);
				if (cipher == null)
					return data;
			}

			// Save off the encoded data for the logging of any input data
			_encodedData = new BitStream();
			data.CopyTo(_encodedData);
			_encodedData.Seek(0, System.IO.SeekOrigin.Begin);

			var len = _encodedData.Length;
			var buf = new BitReader(_encodedData).ReadBytes((int)len);
			_encodedData.Seek(0, System.IO.SeekOrigin.Begin);

			var root = (DataModel)parent.getRoot();
			var context = root.actionData.action.parent.parent.parent.context;
			var sequenceCounter = GetSequenceCounter(context);

			try
			{
				var ret = cipher.DecodeCiphertext(sequenceCounter, ContentType, buf, 0, buf.Length);

				return new BitStream(ret);
			}
			catch (TlsFatalAlert e)
			{
				throw new SoftException(e);
			}
		}

		protected override BitwiseStream internalEncode(BitwiseStream data)
		{
			if (Action == TlsAction.Decrypt)
				return data;

			// Get the cipher.  If it is null, this means we are not in a running action
			var cipher = GetBlockCipher();
			if (cipher == null)
			{
				cipher = CreateBlockCipher(_dataModel.actionData.action.parent.parent.parent.context);
				if (cipher == null)
					return data;
			}

			// If we got encoded data during input, just return that as to not
			// disturb the state of the cipher
			if (_encodedData != null)
				return _encodedData;

			data.Position = 0;

			var len = data.Length - data.Position;
			var buf = new BitReader(data).ReadBytes((int)len);

			var root = (DataModel)parent.getRoot();
			var context = root.actionData.action.parent.parent.parent.context;
			var sequenceCounter = GetSequenceCounter(context);

			Console.WriteLine("Verify Data (Pre-Encrypt): ");
			foreach (var b in buf)
				Console.Write("{0:X2} ", b);
			Console.WriteLine();

			try
			{
				var ret = cipher.EncodePlaintext(sequenceCounter, ContentType, buf, 0, buf.Length);

				return new BitStream(ret);
			}
			catch (TlsFatalAlert e)
			{
				throw new SoftException(e);
			}
		}
	}
}
