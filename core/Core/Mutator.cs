
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
	/// <remarks>
	/// There are two modes of operation for mutators, state model and data model. State model mutators
	/// will affect the flow of the state model and actions in the state model. Data model mutators will
	/// affect the data produced during the current test case.
	/// 
	/// The operation of the mutator is as follows:
	/// 
	/// 1. Your static constructor will correct set the "affectStateModel" and "affectDataModel"
	/// variables. These static variables determine in which modes your mutator will operate.
	/// 
	/// 2. affectDataModel: During record iterations your supportedDataElement static method
	/// will be called to check if your mutator supports a specific dataelement. If true 
	/// an instance of your mutator will be created with the origional instance of the data element
	/// passed in. For every data element in the model you will be queried and asked and an
	/// instance created for that specific element.
	/// 
	///    affectStateModel: A single instance of your mutator will be created and passed a 
	/// the origional state model instance.
	/// 
	/// 3. affectDataModel: During mutation your sequentialMutation or randomMutation methods
	/// will get called with a cloned instance of the origional data element. Each call to these
	/// methods will be passed a new cloned instance that can be modified in any way. In face the
	/// entire model could be modified in any way desired.
	/// 
	///    affectStateModel: During mutation your sequentialMutation or randomMutation methods
	/// will get called with a cloned instance of the origional state model. Each call to these
	/// methods will be passed a new cloned instance that can be modified in any way.
	/// 
	///    affectStateModel: When a changeState action occurs the changeState method is called
	/// to provide an opertunity to modify the state that will be switched to.
	/// </remarks>
	public abstract class Mutator : IWeighted
	{
		/// <summary>
		/// Is this mutator able to affect the state model?
		/// </summary>
		public static bool affectStateModel = false;

		/// <summary>
		/// Is this mutator able to affect the data model?
		/// </summary>
		public static bool affectDataModel = true;

		/// <summary>
		/// Instance of current mutation strategy
		/// </summary>
		public MutationStrategy context = null;

		/// <summary>
		/// Weight this mutator will get chosen in random mutation mode.
		/// </summary>
		public int weight = 1;

		/// <summary>
		/// Name of this mutator
		/// </summary>
		public string name = "Unknown";

		public Mutator()
		{
		}

		public Mutator(DataElement obj)
		{
		}

		public Mutator(StateModel obj)
		{
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
		/// Returns the total number of mutations this
		/// mutator is able to perform.
		/// </summary>
		/// <returns>Returns number of mutations mutater can generate.</returns>
		public abstract int count
		{
			get;
		}

		/// <summary>
		/// Called to set the mutation to return. Applies only to sequencial mutation.
		/// </summary>
		/// <remarks>
		/// This value will range form 0 to count. The same mutation number must always
		/// produce the same mutation form sequencialMutation(). It does not apply to 
		/// randomMutation.
		/// </remarks>
		public abstract uint mutation
		{
			get;
			set;
		}

		/// <summary>
		/// Perform a sequential mutation on a data element
		/// </summary>
		/// <param name="obj">Instance of DataElement for this specific iteration. Each iteration a new instance
		/// of the same object is passed in to be modified.</param>
		/// <remarks>
		/// This method performs mutation algorithm on a specific
		/// data element.
		/// </remarks>
		public abstract void sequentialMutation(DataElement obj);

		/// <summary>
		/// Perform a random mutation on a data element
		/// </summary>
		/// <param name="obj">Instance of DataElement for this specific iteration. Each iteration a new instance
		/// of the same object is passed in to be modified.</param>
		public abstract void randomMutation(DataElement obj);

		/// <summary>
		/// Perform a sequential mutation on state model
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public virtual void sequentialMutation(Core.Dom.StateModel obj)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Perform a random mutation on state model
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public virtual void randomMutation(Core.Dom.StateModel obj)
		{
			throw new NotImplementedException();
		}

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
	}
}
