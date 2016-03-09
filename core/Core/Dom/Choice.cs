
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
using System.Linq;
using System.Xml;

using Peach.Core.Analyzers;
using Peach.Core.IO;
using Peach.Core.Cracker;

using NLog;

namespace Peach.Core.Dom
{
	/// <summary>
	/// Choice allows the selection of a single
	/// data element based on the current data set.
	/// 
	/// The other options in the choice are available
	/// for mutation by the mutators.
	/// </summary>
	[DataElement("Choice")]
	[PitParsable("Choice")]
	[Parameter("name", typeof(string), "Element name", "")]
	[Parameter("fieldId", typeof(string), "Element field ID", "")]
	[Parameter("length", typeof(uint?), "Length in data element", "")]
	[Parameter("lengthType", typeof(LengthType), "Units of the length attribute", "bytes")]
	[Parameter("mutable", typeof(bool), "Is element mutable", "true")]
	[Parameter("constraint", typeof(string), "Scripting expression that evaluates to true or false", "")]
	[Parameter("minOccurs", typeof(int), "Minimum occurrences", "1")]
	[Parameter("maxOccurs", typeof(int), "Maximum occurrences", "1")]
	[Parameter("occurs", typeof(int), "Actual occurrences", "1")]
	[Serializable]
	public class Choice : DataElementContainer
	{
		static NLog.Logger logger = LogManager.GetCurrentClassLogger();
		public NamedCollection<DataElement> choiceElements = new NamedCollection<DataElement>();
		DataElement _selectedElement = null;
		readonly NamedCollection<DataElement> maskedElements = new NamedCollection<DataElement>();

		public Choice()
		{
		}

		public Choice(string name)
			: base(name)
		{
		}

		#region Choice Token Cache

		/// <summary>
		/// Container for cache entries.
		/// </summary>
		[Serializable]
		class ChoiceCache
		{
			/// <summary>
			/// Offset to Token in bits
			/// </summary>
			public long Offset;

			/// <summary>
			/// Token
			/// </summary>
			public BitwiseStream Token;
		}

		/// <summary>
		/// Cache of tokens for fast choice cracking
		/// </summary>
		Dictionary<string, ChoiceCache> _choiceCache = new Dictionary<string, ChoiceCache>();

		class NoChoiceCacheException : Exception { }

		public IEnumerable<DataElement> EnumerateAllElementsDown(DataElementContainer start)
		{
			foreach (var child in start)
			{
				if (child is Choice || child is Array)
					throw new NoChoiceCacheException();

				if (child is DataElementContainer)
				{
					if (child.isDeterministic)
						throw new NoChoiceCacheException();

					foreach (var cchild in EnumerateAllElementsDown(child as DataElementContainer))
						yield return cchild;
				}
				else
				{
					yield return child;
				}
			}
		}

		/// <summary>
		/// Build cache of tokens to speed up choice cracking
		/// </summary>
		public void BuildCache()
		{
			foreach (var elem in choiceElements)
			{
				try
				{
					var cont = elem as DataElementContainer;
					if (cont != null)
					{
						if (cont.isDeterministic)
							throw new NoChoiceCacheException();

						long offset = 0;
						foreach (var child in EnumerateAllElementsDown(cont))
						{
							if (child.isToken)
							{
								logger.Trace("BuildCache: Adding '{0}' as token offset: {1}", child.fullName, offset);
								_choiceCache[elem.Name] = new ChoiceCache() { Offset = offset, Token = child.Value };
								break;
							}

							else if (child.hasLength)
								offset += child.lengthAsBits;

							else
								throw new NoChoiceCacheException();
						}
					}
					else if (elem.isToken)
						_choiceCache[elem.Name] = new ChoiceCache() { Offset = 0, Token = elem.Value };
				}
				catch (NoChoiceCacheException)
				{
					// we end up here if we run info a choice element
				}
			}
		}

		/// <summary>
		/// Check of cached token is in our data stream.
		/// </summary>
		/// <param name="data"></param>
		/// <param name="tok"></param>
		/// <param name="startPosition"></param>
		/// <returns></returns>
		bool TokenCheck(BitStream data, ChoiceCache tok, long startPosition)
		{
			// Enough data?
			if ((startPosition + tok.Offset + tok.Token.LengthBits) > data.LengthBits)
				return false;

			data.PositionBits = startPosition + tok.Offset;
			tok.Token.PositionBits = 0;

			for (int b = 0; (b = tok.Token.ReadByte()) > -1; )
			{
				var bb = data.ReadByte();
				if (bb != b)
					return false;
			}

			for (int b = 0; (b = tok.Token.ReadBit()) > -1; )
			{
				if (data.ReadBit() != b)
					return false;
			}

			return true;
		}

		#endregion

		public override void Crack(DataCracker context, BitStream data, long? size)
		{
			BitStream sizedData = ReadSizedData(data, size);
			long startPosition = sizedData.PositionBits;
			string isTryAfterFailure = null;

			Clear();
			_selectedElement = null;

			// Try our cache (if any) first.
			foreach (var item in _choiceCache)
			{
				if (TokenCheck(sizedData, item.Value, startPosition))
				{
					var child = choiceElements[item.Key];

					try
					{
						logger.Trace("handleChoice: Cache hit for child: {0}", child.debugName);
						context.Log("Cache hit: {0}", child.Name);

						sizedData.SeekBits(startPosition, System.IO.SeekOrigin.Begin);
						context.CrackData(child, sizedData);
						CrackSuccess(child);
						//SelectedElement = child;

						logger.Trace("handleChoice: Keeping child: {0}", child.debugName);
						return;
					}
					catch (CrackingFailure)
					{
						// If we fail to crack the cached option, fall back to the slow method. It's possible
						// there are two tokens in a row and the first one is not deterministic.
						logger.Trace("handleChoice: Failed to crack child using cache. Retrying with slow method...: {0}", child.debugName);
						context.Log("Cache failed, falling back to slow method");
						isTryAfterFailure = item.Key;

						break;
					}
					catch (Exception ex)
					{
						logger.Trace("handleChoice: Child threw exception: {0}: {1}", child.debugName, ex.Message);
						throw;
					}
				}
			}

			// Now try it the slow way
			foreach (DataElement child in choiceElements)
			{
				// Skip any cache entries, already tried them
				// Except if our cache choice failed to parse. Then 
				// try all options except the one we already tried.
				if (isTryAfterFailure == null && _choiceCache.ContainsKey(child.Name))
					continue;

				if (isTryAfterFailure == child.Name)
					continue;

				try
				{
					logger.Trace("handleChoice: Trying child: {0}", child.debugName);

					sizedData.SeekBits(startPosition, System.IO.SeekOrigin.Begin);
					context.CrackData(child, sizedData);
					CrackSuccess(child);
					//SelectedElement = child;

					logger.Trace("handleChoice: Keeping child: {0}", child.debugName);
					return;
				}
				catch (CrackingFailure)
				{
					logger.Trace("handleChoice: Failed to crack child: {0}", child.debugName);
				}
				catch (Exception ex)
				{
					logger.Trace("handleChoice: Child threw exception: {0}: {1}", child.debugName, ex.Message);
					throw;
				}
			}

			throw new CrackingFailure("No valid children were found.", this, data);
		}

		public override void WritePit(XmlWriter pit)
		{
			pit.WriteStartElement(elementType);

			if (referenceName != null)
				pit.WriteAttributeString("ref", referenceName);

			WritePitCommonAttributes(pit);
			WritePitCommonChildren(pit);

			foreach (var obj in this)
				obj.WritePit(pit);

			pit.WriteEndElement();
		}

		public void SelectDefault()
		{
			Clear();
			this.Add(choiceElements[0]);
			_selectedElement = this[0];
		}

		public static DataElement PitParser(PitParser context, XmlNode node, DataElementContainer parent)
		{
			if (node.Name != "Choice")
				return null;

			Choice choice = DataElement.Generate<Choice>(node, parent);
			choice.parent = parent;

			context.handleCommonDataElementAttributes(node, choice);
			context.handleCommonDataElementChildren(node, choice);
			context.handleDataElementContainer(node, choice);

			// Move children to choiceElements collection
			foreach (DataElement elem in choice)
			{
				choice.choiceElements.Add(elem);
				elem.parent = choice;
			}

			choice.Clear();
			choice.BuildCache();

			return choice;
		}

		public override void RemoveAt(int index, bool cleanup)
		{
			// Choices only have a single child, the chosen element
			if (index != 0 || Count != 1)
				throw new ArgumentOutOfRangeException("index");

			// Call clear so that we don't reset the parent
			// of our chosen element.
			// Also reset our selected element so GetChildren()
			// will return our choices.
			_selectedElement = null;
			Clear();

			if (this.Count == 0)
				parent.Remove(this, cleanup);
		}

		public override void ApplyReference(DataElement newElem)
		{
			DataElement oldChoice;
			var idx = choiceElements.Count;

			if (choiceElements.TryGetValue(newElem.Name, out oldChoice))
			{
				idx = choiceElements.IndexOf(oldChoice);
				choiceElements.RemoveAt(idx);
				oldChoice.parent = null;
				newElem.UpdateBindings(oldChoice);
			}

			choiceElements.Insert(idx, newElem);
			newElem.parent = this;
		}

		public DataElement SelectedElement
		{
			get
			{
				return _selectedElement;
			}
			set
			{
				if (!choiceElements.Contains(value))
					throw new KeyNotFoundException("value was not found");

				Clear();
				this.Add(value);
				_selectedElement = value;
				Invalidate();
			}
		}

		internal NamedCollection<DataElement> MaskedElements { get { return maskedElements; } }

		protected override bool InScope(DataElement child)
		{
			return child == SelectedElement;
		}

		protected override IEnumerable<DataElement> Children()
		{
			// Return choices if we haven't chosen yet
			if (_selectedElement != null)
				return base.Children();
			return choiceElements;
		}

		public override IEnumerable<DataElement> DisplayChildren()
		{
			if (maskedElements.Any())
				return maskedElements;
			return choiceElements;
		}

		/// <summary>
		/// Returns a list of children for use in XPath navigation.
		/// Should not be called directly.
		/// </summary>
		/// <returns></returns>
		public override IList<DataElement> XPathChildren()
		{
			return choiceElements;
		}

		protected override DataElement GetChild(string name)
		{
			DataElement ret;
			if (_selectedElement == null)
				choiceElements.TryGetValue(name, out ret);
			else
				TryGetValue(name, out ret);
			return ret;
		}

		protected override Variant GenerateDefaultValue()
		{
			if (SelectedElement == null)
				SelectDefault();

			return new Variant(new BitStreamList(new[] { SelectedElement.Value }));
		}

		private void CrackSuccess(DataElement child)
		{
			var dm = root as DataModel;
			if (dm != null && dm.actionData != null && dm.actionData.IsOutput && IntPtr.Size == 8)
			{
				// If this is an output action on a 64-bit machine,
				// we will do normal selection and remember our alternative choices
				SelectedElement = child;
				return;
			}

			try
			{
				// Remove all references to all non-chosen choice children
				BeginUpdate();

				Clear();

				foreach (var item in choiceElements.Where(i => i != child))
				{
					Add(item);
					base.RemoveAt(0, true);
				}

				Add(child);

				_choiceCache.Clear();
				choiceElements.Clear();
				choiceElements.Add(child);
				_selectedElement = child;
			}
			finally
			{
				EndUpdate();
			}
		}
	}
}

// end
