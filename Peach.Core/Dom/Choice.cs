
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
using System.Collections;
using System.Text;
using System.Runtime.InteropServices;
using System.Runtime;
using System.Reflection;
using System.Runtime.Serialization;
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
	[Parameter("length", typeof(uint?), "Length in data element", "")]
	[Parameter("lengthType", typeof(LengthType), "Units of the length attribute", "bytes")]
	[Parameter("mutable", typeof(bool), "Is element mutable", "true")]
	[Parameter("constraint", typeof(string), "Scripting expression that evaluates to true or false", "")]
	[Parameter("minOccurs", typeof(int), "Minimum occurances", "1")]
	[Parameter("maxOccurs", typeof(int), "Maximum occurances", "1")]
	[Parameter("occurs", typeof(int), "Actual occurances", "1")]
	[Serializable]
	public class Choice : DataElementContainer
	{
		static NLog.Logger logger = LogManager.GetCurrentClassLogger();
		public OrderedDictionary<string, DataElement> choiceElements = new OrderedDictionary<string, DataElement>();
		DataElement _selectedElement = null;

		public Choice()
		{
		}

		public Choice(string name)
			: base(name)
		{
		}

		public override void Crack(DataCracker context, BitStream data, long? size)
		{
			BitStream sizedData = ReadSizedData(data, size);
			long startPosition = sizedData.PositionBits;

			Clear();
			_selectedElement = null;

			foreach (DataElement child in choiceElements.Values)
			{
				try
				{
					logger.Debug("handleChoice: Trying child: " + child.debugName);

					sizedData.SeekBits(startPosition, System.IO.SeekOrigin.Begin);
					context.CrackData(child, sizedData);
					SelectedElement = child;

					logger.Debug("handleChoice: Keeping child: " + child.debugName);
					return;
				}
				catch (CrackingFailure)
				{
					logger.Debug("handleChoice: Failed to crack child: " + child.debugName);
				}
				catch (Exception ex)
				{
					logger.Debug("handleChoice: Child threw exception: " + child.debugName + ": " + ex.Message);
					throw;
				}
			}

			throw new CrackingFailure(debugName + " has no valid children.", this, data);
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
				choice.choiceElements.Add(elem.name, elem);
				elem.parent = choice;
			}

			choice.Clear();

			return choice;
		}

		public override void RemoveAt(int index)
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
				parent.Remove(this);
		}

		public override void ApplyReference(DataElement newElem)
		{
			DataElement oldChoice;

			if (choiceElements.TryGetValue(newElem.name, out oldChoice))
			{
				oldChoice.parent = null;
				newElem.UpdateBindings(oldChoice);
			}

			choiceElements[newElem.name] = newElem;
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
				if (!choiceElements.Values.Contains(value))
					throw new KeyNotFoundException("value was not found");

				Clear();
				this.Add(value);
				_selectedElement = value;
				Invalidate();
			}
		}

		protected override IEnumerable<DataElement> Children()
		{
			// Return choices if we haven't chosen yet
			if (_selectedElement != null)
				return base.Children();
			else
				return choiceElements.Values;
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

			return new Variant(SelectedElement.Value);
		}
	}
}

// end
