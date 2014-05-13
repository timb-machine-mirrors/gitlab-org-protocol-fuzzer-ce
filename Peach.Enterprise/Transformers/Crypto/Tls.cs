using System;
using System.Collections.Generic;
using System.Reflection;

using Peach.Core;
using Peach.Core.IO;
using Peach.Core.Dom;

using Org.BouncyCastle.Security;
using Org.BouncyCastle.Crypto.Tls;
using NLog;

namespace Peach.Enterprise.Transformers.Crypto
{
	[Description("Tls Transformer")]
	[Transformer("Tls", true)]
	[Parameter("ContentType", typeof(byte), "Type of message to encrypt/decrypt")]
	[Serializable]
	public class Tls : Transformer
	{
		BitStream encodedData;

		DataModel dataModel;

		public byte ContentType { get; set; }

		public Tls(DataElement parent, Dictionary<string, Variant> args)
			: base(parent, args)
		{
			ParameterParser.Parse(this, args);
		}

		TlsBlockCipher GetBlockCipher()
		{
			if (dataModel == null)
			{
				dataModel = parent.getRoot() as DataModel;
				dataModel.ActionRun += OnActionRunEvent;
			}

			// We are not running inside an action!
			if (dataModel.actionData == null)
				return null;

			var ctx = dataModel.actionData.action.parent.parent.parent.context;

			// The Tls fixup needs to have run first or this transformer is useless
			return (TlsBlockCipher)ctx.iterationStateStore["TlsBlockCipher"];
		}

		// Called every time an output action occurs
		void OnActionRunEvent(RunContext ctx)
		{
			parent.Invalidate();
		}

		[OnCloned]
		void OnCloned(Tls original, object context)
		{
			// The event is not serialized so we need to
			// resubscribe when we are cloned.
			if (dataModel != null)
				dataModel.ActionRun += OnActionRunEvent;
		}

		protected override BitStream internalDecode(BitStream data)
		{
			// Get the cipher.  If it is null, this means we are not in a running action
			var cipher = GetBlockCipher();
			if (cipher == null)
				return data;

			// Save off the encoded data for the logging of any input data
			encodedData = new BitStream();
			data.CopyTo(encodedData);
			encodedData.Seek(0, System.IO.SeekOrigin.Begin);

			var len = encodedData.Length;
			var buf = new BitReader(encodedData).ReadBytes((int)len);
			encodedData.Seek(0, System.IO.SeekOrigin.Begin);

			var ret = cipher.DecodeCiphertext((ContentType)ContentType, buf, 0, buf.Length);

			return new BitStream(ret);
		}

		protected override BitwiseStream internalEncode(BitwiseStream data)
		{
			// Get the cipher.  If it is null, this means we are not in a running action
			var cipher = GetBlockCipher();
			if (cipher == null)
				return data;

			// If we got encoded data during input, just return that as to not
			// disturb the state of the cipher
			if (encodedData != null)
				return encodedData;

			var len = data.Length - data.Position;
			var buf = new BitReader(data).ReadBytes((int)len);

			var ret = cipher.EncodePlaintext((ContentType)ContentType, buf, 0, buf.Length);

			return new BitStream(ret);
		}
	}
}
