
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
using Peach.Core.Dom;

namespace Peach.Core
{
	/// <summary>
	/// Base class for Mutators.
	/// </summary>
	public abstract class Mutator : IWeighted, INamed
	{
		/// <summary>
		/// Instance of current mutation strategy
		/// </summary>
		public MutationStrategy context = null;

		/// <summary>
		/// Weight this mutator will get chosen in random mutation mode.
		/// </summary>
		public int weight = 1;

		public Mutator()
		{
		}

		public Mutator(DataElement obj)
		{
		}

		public Mutator(State obj)
		{
		}

		/// <summary>
		/// The name of the mutator
		/// </summary>
		public virtual string name
		{
			get
			{
				return GetType().Name;
			}
		}

		/// <summary>
		/// Check to see if DataElement is supported by this 
		/// mutator.
		/// </summary>
		/// <param name="obj">DataElement to check</param>
		/// <returns>True if object is supported, else False</returns>
		public static bool supportedDataElement(DataElement obj)
		{
			return false;
		}

		/// <summary>
		/// Check to see if State is supported by this 
		/// mutator.
		/// </summary>
		/// <param name="obj">State to check</param>
		/// <returns>True if object is supported, else False</returns>
		public static bool supportedState(State obj)
		{
			return false;
		}

		/// <summary>
		/// Returns the total number of mutations this
		/// mutator is able to perform.
		/// </summary>
		/// <returns>Returns number of mutations mutater can generate.</returns>
		public abstract int count
		{
			get;
		}

		public abstract uint mutation
		{
			get;
			set;
		}

		/// <summary>
		/// Perform a sequential mutation.
		/// </summary>
		/// <param name="obj"></param>
		public abstract void sequentialMutation(DataElement obj);

		/// <summary>
		/// Perform a random mutation.
		/// </summary>
		/// <param name="obj"></param>
		public abstract void randomMutation(DataElement obj);

		/// <summary>
		/// Allow changing which state we change to.
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public virtual State changeState(State obj)
		{
			throw new NotImplementedException();
		}

		#region IWeighted Members

		public int SelectionWeight
		{
			get { return weight; }
		}

		#endregion

		protected static string getHint(DataElement obj, string name)
		{
			Hint h;

			if (!obj.Hints.TryGetValue(name, out h))
				return null;

			return h.Value;
		}

		protected static bool getTypeTransformHint(DataElement obj)
		{
			var name = "Peach.TypeTransform";
			var value = getHint(obj, name);

			// Defaults to supporting type transforms
			if (string.IsNullOrEmpty(value))
				return true;

			bool ret;

			if (!bool.TryParse(value.ToLower(), out ret))
				throw new PeachException("{0} hint '{1}' has invalid value '{2}'.".Fmt(obj.debugName, name, value));
			
			return ret;
		}

		protected bool getN(DataElement obj, out uint n)
		{
			return getN(obj, GetType().Name, out n);
		}

		protected bool getN(DataElement obj, string prefix, out uint n)
		{
			var name = prefix + "-N";
			var value = getHint(obj, name);

			if (string.IsNullOrEmpty(value))
			{
				n = 0;
				return false;
			}

			if (!uint.TryParse(value, out n))
				throw new PeachException("{0} hint '{1}' has invalid value '{2}'.".Fmt(obj.debugName, name, value));

			return true;
		}

		protected uint getN(DataElement obj, uint defaultValue)
		{
			uint ret;
			if (!getN(obj, out ret))
				ret = defaultValue;
			return ret;
		}
	}
}
