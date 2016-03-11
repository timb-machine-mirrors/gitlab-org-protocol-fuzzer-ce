using System;
using System.Collections.Generic;
using Org.BouncyCastle.Crypto.Tls;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.IO;
using Peach.Pro.Core.Dom;
using Peach.Pro.Core.Fixups;
using DescriptionAttribute = System.ComponentModel.DescriptionAttribute;

namespace Peach.Pro.Core.Transformers.Crypto
{
	[Description("DTls Transformer")]
	[Transformer("DTls", true, Internal = true)]
	[Parameter("ContentType", typeof(byte), "Type of message to encrypt/decrypt")]
	[Parameter("IsServer", typeof(bool), "Is server?", "false")]
	[Parameter("Action", typeof(TlsAction), "Action to perform [Both, Encrypt, Decrypt]", "Both")]
	[Parameter("TlsVersion", typeof(TlsVersion), "TLS Version", "DTLSv10")]
	[Serializable]
	public class DTls : Transformer
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
					case TlsVersion.TLSv11:
					case TlsVersion.TLSv12:
						throw new PeachException("DTls transformer only supports DTLS versions.");
					case TlsVersion.DTLSv10:
						return ProtocolVersion.DTLSv10;
					case TlsVersion.DTLSv12:
						return ProtocolVersion.DTLSv12;
					default:
						throw new PeachException("DTls transformer does not yet support selected TlsVersion.");
				}
			}
		}

		public DTls(DataElement parent, Dictionary<string, Variant> args)
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
		}

		long GetSequenceCounter(RunContext ctx)
		{
			DataElement epochElement;
			DataElement seqElement;

			try
			{
				var frag = (Frag)((DataElementContainer)parent.getRoot())["FragPacket"];

				if (Action == TlsAction.Encrypt || Action== TlsAction.Both)
				{
					epochElement = ((DataElementContainer) frag.Template)["Epoch"];
					seqElement = ((DataElementContainer) frag.Template)["SequenceNumber"];
				}
				else
				{
					epochElement = parent.getRoot().find("Epoch");
					seqElement = parent.getRoot().find("SequenceNumber");
				}
			}
			catch (Exception)
			{
				epochElement = parent.getRoot().find("Epoch");
				seqElement = parent.getRoot().find("SequenceNumber");
			}

			try
			{

				if (epochElement == null)
					throw new SoftException("DTls transformer can't find Epoch element.");

				var epoch = (int)epochElement.InternalValue;

				if (seqElement == null)
					throw new SoftException("DTls transfomer can't find SequenceNumber element.");

				var sequenceNumber = (ulong)seqElement.InternalValue;

				var bitStream = new BitStream();
				var writer = new BitWriter(bitStream);
				writer.BigEndian();

				writer.WriteUInt16((ushort)epoch);
				writer.WriteBits(sequenceNumber, 48);

				bitStream.Position = 0;
				var reader = new BitReader(bitStream);
				reader.BigEndian();

				return reader.ReadInt64();
			}
			catch (NotSupportedException)
			{
				// Thrown when trying to convert Variant to int
				return 0;
			}
			catch (ApplicationException)
			{
				// Thrown when conversion would losse information
				return 0;
			}
		}

		[OnCloned]
		void OnCloned(DTls original, object context)
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

			try
			{
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

			try
			{
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

				//Console.WriteLine("Verify Data (Pre-Encrypt): ");
				//foreach (var b in buf)
				//	Console.Write("{0:X2} ", b);
				//Console.WriteLine();

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
