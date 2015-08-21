using System;
using System.Collections.Generic;
using Org.BouncyCastle.Crypto.Tls;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.IO;
using DescriptionAttribute = System.ComponentModel.DescriptionAttribute;

namespace Peach.Pro.Core.Transformers.Crypto
{
	[Description("DTls Transformer")]
	[Transformer("DTls", true, Internal = true)]
	[Parameter("ContentType", typeof(byte), "Type of message to encrypt/decrypt")]
	[Serializable]
	public class DTls : Transformer
	{
		BitStream _encodedData;
		DataModel _dataModel;

		public byte ContentType { get; set; }

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

		//static T GetState<T>(RunContext ctx, string key) where T : class
		//{
		//	object obj;
		//	if (ctx.iterationStateStore.TryGetValue(key, out obj))
		//		return (T)obj;
		//	return default(T);
		//}

		// Called every time an output action occurs
		void OnActionRunEvent(RunContext ctx)
		{
			parent.Invalidate();
		}

		long GetSequenceCounter()
		{
			var epochElement = parent.getRoot().find("Epoch");
			if (epochElement == null)
				throw new SoftException("DTls transformer can't find Epoch element.");

			var epoch = (int)epochElement.InternalValue;

			var seqElement = parent.getRoot().find("SequenceNumber");
			if (seqElement == null)
				throw new SoftException("DTls transfomer can't find SequenceNumber element.");

			var sequenceNumber = (int)seqElement.InternalValue;

			var bitStream = new BitStream(new byte[8]);
			var writer = new BitWriter(bitStream);

			writer.WriteUInt16((ushort)epoch);
			writer.WriteBits((ulong)sequenceNumber, 48);

			bitStream.Position = 0;
			var reader = new BitReader(bitStream);

			return reader.ReadInt64();
		}

		[OnCloned]
		void OnCloned(DTls original, object context)
		{
			// The event is not serialized so we need to
			// resubscribe when we are cloned.
			if (_dataModel != null)
				_dataModel.ActionRun += OnActionRunEvent;
		}

		protected override BitStream internalDecode(BitStream data)
		{
			// Get the cipher.  If it is null, this means we are not in a running action
			var cipher = GetBlockCipher();
			if (cipher == null)
				return data;

			// Save off the encoded data for the logging of any input data
			_encodedData = new BitStream();
			data.CopyTo(_encodedData);
			_encodedData.Seek(0, System.IO.SeekOrigin.Begin);

			var len = _encodedData.Length;
			var buf = new BitReader(_encodedData).ReadBytes((int)len);
			_encodedData.Seek(0, System.IO.SeekOrigin.Begin);

			var sequenceCounter = GetSequenceCounter();

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
			// Get the cipher.  If it is null, this means we are not in a running action
			var cipher = GetBlockCipher();
			if (cipher == null)
				return data;

			// If we got encoded data during input, just return that as to not
			// disturb the state of the cipher
			if (_encodedData != null)
				return _encodedData;

			data.Position = 0;

			var len = data.Length - data.Position;
			var buf = new BitReader(data).ReadBytes((int)len);

			try
			{
				var ret = cipher.EncodePlaintext(GetSequenceCounter(), ContentType, buf, 0, buf.Length);

				return new BitStream(ret);
			}
			catch (TlsFatalAlert e)
			{
				throw new SoftException(e);
			}
		}
	}
}
