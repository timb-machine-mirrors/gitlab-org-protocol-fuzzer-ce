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
//   Michael Eddington (mike@phed.org)

// $Id$

using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using Peach.Core.Agent;
using System.Runtime.Serialization;

namespace Peach.Core.Dom
{
	[Serializable]
	public class Dom : INamed
    {
		public string fileName;
		public string version;
		public string author;
		public string description;

		public RunContext context = null;
		public OrderedDictionary<string, DomNamespace> ns = new OrderedDictionary<string, DomNamespace>();
		public OrderedDictionary<string, DataModel> dataModels = new OrderedDictionary<string, DataModel>();
		public OrderedDictionary<string, StateModel> stateModels = new OrderedDictionary<string, StateModel>();
		public OrderedDictionary<string, Agent> agents = new OrderedDictionary<string, Agent>();
		public OrderedDictionary<string, Test> tests = new OrderedDictionary<string, Test>();
		public OrderedDictionary<string, Run> runs = new OrderedDictionary<string, Run>();

		public Dom()
		{
			ns.AddEvent += new AddEventHandler<string, DomNamespace>(ns_AddEvent);
			dataModels.AddEvent += new AddEventHandler<string, DataModel>(dataModels_AddEvent);
			stateModels.AddEvent += new AddEventHandler<string, StateModel>(stateModels_AddEvent);
			agents.AddEvent += new AddEventHandler<string, Agent>(agents_AddEvent);
			tests.AddEvent += new AddEventHandler<string, Test>(tests_AddEvent);
			runs.AddEvent += new AddEventHandler<string, Run>(runs_AddEvent);
		}

		void agents_AddEvent(OrderedDictionary<string, Agent> sender, string key, Agent value)
		{
		}

		#region OrderedDictionary AddEvent Handlers

		void runs_AddEvent(OrderedDictionary<string, Run> sender, string key, Run value)
		{
			value.parent = this;
		}

		void tests_AddEvent(OrderedDictionary<string, Test> sender, string key, Test value)
		{
			value.parent = this;
		}

		void stateModels_AddEvent(OrderedDictionary<string, StateModel> sender, string key, StateModel value)
		{
			value.parent = this;
		}

		void dataModels_AddEvent(OrderedDictionary<string, DataModel> sender, string key, DataModel value)
		{
			value.dom = this;
		}

		void ns_AddEvent(OrderedDictionary<string, DomNamespace> sender, string key, DomNamespace value)
		{
			value.parent = this;
		}

		#endregion


		#region INamed Members

		public virtual string name
		{
			get { return "root"; }
			set { throw new Exception("Error, Dom.name is static and cannot be set"); }
		}

		#endregion
	}
}


// END
