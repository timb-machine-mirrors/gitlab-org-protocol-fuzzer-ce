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
using System.Collections.ObjectModel;
using System.IO;
using System.Text.RegularExpressions;

using Peach.Core.Cracker;
using Peach.Core.IO;

namespace Peach.Core.Dom
{
	/// <summary>
	/// Interface for Data
	/// </summary>
	public interface Data : INamed
	{
		/// <summary>
		/// Applies the Data to the specified data model
		/// </summary>
		/// <param name="model"></param>
		void Apply(DataModel model);

		/// <summary>
		/// Will this data set be ignored by the engine when
		/// looking for a new data set to switch to.
		/// </summary>
		bool Ignore { get; set; }

		string FieldId { get; }
	}

	/// <summary>
	/// Data that comes from a file
	/// </summary>
	[Serializable]
    public class DataFile : DataField
	{
		public DataFile(DataSet dataSet, string fileName) : base(dataSet)
		{
			Name = "{0}/{1}".Fmt(dataSet.Name, Path.GetFileName(fileName));
			FileName = fileName;
		}

		public override void Apply(DataModel model)
		{
			try
			{
				using (var fs = File.OpenRead(FileName))
				{
					Stream strm = fs;

					// If the sample file is < 16Mb, copy it to
					// a MemoryStream to speedup seeking during
					// cracking of lots of choices.
					if (fs.Length < 1024 * 1024 * 16)
					{
						var ms = new MemoryStream();
						fs.CopyTo(ms);
						ms.Seek(0, SeekOrigin.Begin);
						strm = ms;
					}

					var cracker = new DataCracker();
					cracker.CrackData(model, new BitStream(strm));

                    // Apply field values
                    base.Apply(model);
                }
			}
			catch (CrackingFailure ex)
			{
				throw new PeachException("Error, failed to crack \"" + FileName +
					"\" into \"" + model.fullName + "\": " + ex.Message, ex);
			}
		}

		public string FileName
		{
			get;
			private set;
		}
	}

	/// <summary>
	/// Data that comes from fields
	/// </summary>
	[Serializable]
	public class DataField : Data
	{
		#region Obsolete Functions

		[Obsolete("This property is obsolete and has been replaced by the Name property.")]
		public string name { get { return Name; } }

		#endregion

		[Serializable]
		public class Field
		{
			public string Name { get; set; }
			public Variant Value { get; set; }
		}

		[Serializable]
		public class FieldCollection : KeyedCollection<string, Field>
		{
			protected override string GetKeyForItem(Field item)
			{
				return item.Name;
			}
		}

		public DataField(DataSet dataSet)
		{
			Name = dataSet.Name;
			FieldId = dataSet.FieldId;
			Fields = new FieldCollection();
		}

		public string Name
		{
			get;
			protected set;
		}

		public string FieldId
		{
			get;
			protected set;
		}

		public FieldCollection Fields
		{
			get;
			protected set;
		}

		public bool Ignore
		{
			get;
			set;
		}

		public virtual void Apply(DataModel model)
		{
			// Examples of valid field names:
			//
			//  1. foo
			//  2. foo.bar
			//  3. foo[N].bar[N].foo
			//

			foreach (var kv in Fields)
			{
				ApplyField(model, kv.Name, kv.Value);
			}

			model.evaulateAnalyzers();
		}

		static protected void ApplyField(DataElementContainer model, string field, Variant value)
		{
			DataElement elem = model;
			DataElementContainer container = model;
			var names = field.Split('.');

			for (int i = 0; i < names.Length; i++)
			{
				string name = names[i];
				Match m = Regex.Match(name, @"(.*)\[(-?\d+)\]$");

				if (m.Success)
				{
					name = m.Groups[1].Value;
					int index = int.Parse(m.Groups[2].Value);

					if (!container.TryGetValue(name, out elem))
						throw new PeachException("Error, unable to resolve field \"" + field + "\" against \"" + model.fullName + "\".");

					var seq = elem as Sequence;
					if (seq == null)
						throw new PeachException("Error, cannot use array index syntax on field name unless target element is an array. Field: " + field);
					
					var array = elem as Array;
					if (array != null)
					{
						// Are we disabling this array?
						if (index == -1)
						{
							if (array.minOccurs > 0)
								throw new PeachException("Error, cannot set array to zero elements when minOccurs > 0. Field: " + field + " Element: " + array.fullName);

							// Mark array as expanded
							array.ExpandTo(0);

							// The field should be applied to a template data model so
							// the array should have never had any elements in it.
							// Only the original element should be set.
							System.Diagnostics.Debug.Assert(array.Count == 0);

							return;
						}

						if (array.maxOccurs != -1 && index > array.maxOccurs)
							throw new PeachException("Error, index larger that maxOccurs.  Field: " + field + " Element: " + array.fullName);

						// Add elements up to our index
						array.ExpandTo(index + 1);
					}
					else
					{
						if (index < 0)
							throw new PeachException("Error, index must be equal to or greater than 0");
						if (index > seq.Count - 1)
							throw new PeachException("Error, array index greater than the number of elements in sequence");
					}

					elem = seq[index];
					container = elem as DataElementContainer;
				}
				else if (container is Choice)
				{
					elem = null;
					var choice = container as Choice;
					if (!choice.choiceElements.TryGetValue(name, out elem))
						throw new PeachException("Error, unable to resolve field \"" + field + "\" against \"" + model.fullName + "\".");

					container = elem as DataElementContainer;

					choice.SelectedElement = elem;
				}
				else
				{
					if (container == null || !container.TryGetValue(name, out elem))
						throw new PeachException("Error, unable to resolve field \"" + field + "\" against \"" + model.fullName + "\".");

					container = elem as DataElementContainer;
				}
			}

			if (elem.parent is Choice && string.IsNullOrEmpty(value.ToString()))
				return;

			if (!(elem is DataElementContainer))
			{
				if (value.GetVariantType() == Variant.VariantType.BitStream)
					((BitwiseStream)value).Seek(0, SeekOrigin.Begin);

				elem.DefaultValue = value;
			}
		}
	}
}

