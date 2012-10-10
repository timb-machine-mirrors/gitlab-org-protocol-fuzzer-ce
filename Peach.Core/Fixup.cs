﻿
//
// Copyright (c) Michael Eddington
//
// Permission is hereby granted, free of charge, to any person obtaining a copy 
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights 
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
// copies of the Software, and to permit persons to whom the Software is 
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in	
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//

// Authors:
//   Michael Eddington (mike@dejavusecurity.com)

// $Id$

using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Peach.Core.Dom;
using System.Runtime.Serialization;

namespace Peach.Core
{
	[Serializable]
	public abstract class Fixup
	{
		protected Dictionary<string, Variant> args;
		protected bool isRecursing = false;
		protected DataElement parent = null;

		[NonSerialized]
		protected Dictionary<string, DataElement> elements = null;

		// Needed for re-subscribing the Invalidated event on deserialize
		private List<Tuple<string, DataElement>> refs = new List<Tuple<string, DataElement>>();
		private bool resolvedRefs = false;

		public Fixup(DataElement parent, Dictionary<string, Variant> args, params string[] refs)
		{
			this.parent = parent;
			this.args = args;

			if (!refs.SequenceEqual(refs.Intersect(args.Keys)))
			{
				string msg = string.Format("Error, {0} requires a '{1}' argument!",
					this.GetType().Name,
					string.Join("' AND '", refs));

				throw new PeachException(msg);
			}

			foreach (var item in refs)
				this.refs.Add(new Tuple<string, DataElement>(item, null));
		}

		public Dictionary<string, Variant> arguments
		{
			get { return args; }
			set { args = value; }
		}

		/// <summary>
		/// Perform fixup operation
		/// </summary>
		/// <param name="obj">Parent data element</param>
		/// <returns></returns>
		public Variant fixup(DataElement obj)
		{
			if (isRecursing)
				return obj.DefaultValue;

			try
			{
				isRecursing = true;
				return doFixupImpl(obj);
			}
			finally
			{
				isRecursing = false;
			}
		}

		private Variant doFixupImpl(DataElement obj)
		{
			System.Diagnostics.Debug.Assert(parent != null);

			if (!resolvedRefs)
			{
				resolvedRefs = true;

				System.Diagnostics.Debug.Assert(elements == null);
				elements = new Dictionary<string,DataElement>();

				for (int i = 0; i < refs.Count; ++i)
				{
					System.Diagnostics.Debug.Assert(refs[i].Item2 == null);
					string refName = refs[i].Item1;
					string elemName = (string)args[refName];

					var elem = obj.find(elemName);
					if (elem == null)
						throw new PeachException(string.Format("{0} could not find ref element '{1}'", this.GetType().Name, elemName));

					elem.Invalidated += new InvalidatedEventHandler(OnInvalidated);
					elements.Add(refName, elem);
					refs[i] = new Tuple<string,DataElement>(refName, elem);
				}
			}

			return fixupImpl();
		}

		private void OnInvalidated(object sender, EventArgs e)
		{
			parent.Invalidate();
		}

		[Serializable]
		class FullName
		{
			public FullName(string refName, string fullName)
			{
				this.refName = refName;
				this.fullName = fullName;
			}

			public string refName;
			public string fullName;
		}

		private List<FullName> fullNames = null;

		class Metadata : Dictionary<string, DataElement> {}

		[OnSerializing]
		private void OnSerializing(StreamingContext context)
		{
			DataElement.CloneContext ctx = context.Context as DataElement.CloneContext;
			if (ctx == null)
				return;

			System.Diagnostics.Debug.Assert(fullNames == null);
			fullNames = new List<FullName>();

			// Nothing to do if we haven't resolved any data elements yet
			if (!resolvedRefs)
				return;

			System.Diagnostics.Debug.Assert(!ctx.metadata.ContainsKey(this));
			Metadata m = new Metadata();

			for (int i = 0; i < refs.Count; ++i)
			{
				string relName;
				var name = refs[i].Item1;
				var elem = refs[i].Item2;

				// Fixup references an element that is not a child of ctx.root
				if (elem != null && elem != ctx.root && !elem.isChildOf(ctx.root, out relName))
				{
					ctx.elements[relName] = elem;
					m.Add(name, elem);
					fullNames.Add(new FullName(name, relName));
					refs[i] = new Tuple<string, DataElement>(name, null);
				}
			}

			if (m.Count > 0)
				ctx.metadata.Add(this, m);
		}

		[OnSerialized]
		private void OnSerialized(StreamingContext context)
		{
			DataElement.CloneContext ctx = context.Context as DataElement.CloneContext;
			if (ctx == null)
				return;

			System.Diagnostics.Debug.Assert(fullNames != null);
			fullNames = null;

			if (!resolvedRefs)
				return;

			for (int i = 0; i < refs.Count; ++i)
			{
				if (refs[i].Item2 == null)
				{
					string tgt = refs[i].Item1;
					refs[i] = new Tuple<string, DataElement>(tgt, elements[tgt]);
				}
			}
		}

		[OnDeserialized]
		private void OnDeserialized(StreamingContext context)
		{
			DataElement.CloneContext ctx = context.Context as DataElement.CloneContext;
			if (ctx == null)
				return;

			// DataElement.Invalidated is not serialized, so re-subscribe to the event
			// Can't use the Dictionary, must use a list
			// See: http://stackoverflow.com/questions/457134/strange-behaviour-of-net-binary-serialization-on-dictionarykey-value

			System.Diagnostics.Debug.Assert(fullNames != null);

			// If we haven't resolved any references yet, there is nothing to do
			if (!resolvedRefs)
			{
				fullNames = null;
				return;
			}

			System.Diagnostics.Debug.Assert(elements == null);
			elements = new Dictionary<string, DataElement>();

			for (int i = 0; i < refs.Count; ++i)
			{
				if (refs[i].Item2 == null)
				{
					// DataElement is not a child of ctx.root, resolve it
					string tgt = refs[i].Item1;
					var rec = fullNames.Find(v => v.refName == tgt);
					System.Diagnostics.Debug.Assert(rec != null);
					var elem = ctx.elements[rec.fullName];
					System.Diagnostics.Debug.Assert(elem != null);
					refs[i] = new Tuple<string, DataElement>(tgt, elem);
				}

				refs[i].Item2.Invalidated += new InvalidatedEventHandler(OnInvalidated);
				elements.Add(refs[i].Item1, refs[i].Item2);
			}

			fullNames = null;
		}

		protected abstract Variant fixupImpl();
	}

	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
	public class FixupAttribute : PluginAttribute
	{
		public string description;
		public bool isDefault;

		public FixupAttribute(string name, string description, bool isDefault = false)
			: base(name)
		{
			this.description = description;
			this.isDefault = isDefault;
		}
	}
}

// end
