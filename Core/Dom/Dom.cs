
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
using Peach.Core.Agent;
using System.Runtime.Serialization;
using System.Xml;

namespace Peach.Core.Dom
{
	public class Dom : INamed
	{
		/// <summary>
		/// The namespace of this Dom.
		/// </summary>
		public string name { get; set; }

		public string fileName { get; set; }
		public string version { get; set; }
		public string author { get; set; }
		public string description { get; set; }

		public RunContext context { get; set; }

		public OwnedCollection<Dom, DataModel> dataModels { get; private set; }
		public OwnedCollection<Dom, StateModel> stateModels { get; private set; }
		public OwnedCollection<Dom, Test> tests { get; private set; }
		public NamedCollection<Dom> ns { get; private set; }
		public NamedCollection<Agent> agents { get; private set; }
		public NamedCollection<DataSet> datas { get; private set; }

		public Scripting Python { get; private set; }
		public Scripting Ruby { get; private set; }

		public Dom()
		{
			name = "";
			fileName = "";
			version = "";
			author = "";
			description = "";

			dataModels = new OwnedCollection<Dom, DataModel>(this);
			stateModels = new OwnedCollection<Dom, StateModel>(this);
			tests = new OwnedCollection<Dom, Test>(this);
			ns = new NamedCollection<Dom>();
			agents = new NamedCollection<Agent>();
			datas = new NamedCollection<DataSet>();

			Python = new PythonScripting();
			Ruby = new RubyScripting();
		}

		/// <summary>
		/// Execute all analyzers on all data models in DOM.
		/// </summary>
		public void evaulateDataModelAnalyzers()
		{
			foreach (var model in dataModels)
				model.evaulateAnalyzers();

			foreach (var test in tests)
			{
				foreach (var state in test.stateModel.states)
				{
					foreach (var action in state.actions)
					{
						foreach (var data in action.allData)
						{
							data.dataModel.evaulateAnalyzers();
						}
					}
				}
			}
		}

		#region Reference Resolution

		/// <summary>
		/// Find a referenced Dom element by name, taking into account namespace prefixes.
		/// </summary>
		/// <typeparam name="T">Type of Dom element.</typeparam>
		/// <param name="refName">Name of reference</param>
		/// <param name="predicate">Selector predicate that returns the element collection</param>
		/// <returns>The named Dom element or null if not found.</returns>
		public T getRef<T>(string refName, Func<Dom, ITryGetValue<string, T>> predicate)
		{
			int i = refName.IndexOf(':');
			if (i > -1)
			{
				string prefix = refName.Substring(0, i);

				Dom other;
				if (!ns.TryGetValue(prefix, out other))
					throw new PeachException("Unable to locate namespace '" + prefix + "' in ref '" + refName + "'.");

				refName = refName.Substring(i + 1);

				return other.getRef<T>(refName, predicate);
			}

			var dict = predicate(this);
			T value = default(T);
			if (dict.TryGetValue(refName, out value))
				return value;
			return default(T);
		}

		/// <summary>
		/// Resolve a 'ref' attribute.  Will throw a PeachException if
		/// namespace is given, but not found.
		/// </summary>
		/// <param name="name">Ref name to resolve.</param>
		/// <param name="element">Container to start searching from.</param>
		/// <returns>DataElement for ref or null if not found.</returns>
		public DataElement getRef(string name, DataElementContainer element)
		{
			return getRef(this, name, element);
		}

		static DataElement getRef(Dom dom, string name, DataElementContainer container)
		{
			if (name.IndexOf(':') > -1)
			{
				string ns = name.Substring(0, name.IndexOf(':'));

				Dom other;
				if (!dom.ns.TryGetValue(ns, out other))
					throw new PeachException("Unable to locate namespace '" + ns + "' in ref '" + name + "'.");

				name = name.Substring(name.IndexOf(':') + 1);

				// If we have to look in a different namespace, ignore any container
				// that might have been passed to us
				return getRef(other, name, null);
			}

			if (container != null)
			{
				DataElement elem = container.find(name);
				if (elem != null)
					return elem;
			}

			foreach (DataModel model in dom.dataModels)
			{
				if (model.name == name)
					return model;
			}

			foreach (DataModel model in dom.dataModels)
			{
				DataElement elem = model.find(name);
				if (elem != null)
					return elem;
			}

			return null;
		}

		#endregion
	}
}


// END
