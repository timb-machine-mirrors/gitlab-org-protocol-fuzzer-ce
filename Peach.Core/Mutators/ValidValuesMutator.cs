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
using System.Text;
using Peach.Core.Dom;

namespace Peach.Core.Mutators
{
    [Mutator("Allows different valid values to be specified")]
    [Hint("ValidValues", "Provide additional values for element separeted with ;.")]
	public class ValidValuesMutator : Mutator
	{
        // members
        //
        int pos = 0;
        int max = 0;
        string[] values = new string[] { };

        // CTOR
        //
        public ValidValuesMutator(DataElement obj)
        {
            pos = 0;
            generateValues(obj);
        }

        // GENERATE VALUES
        //
        public void generateValues(DataElement obj)
        {
            // 1. Get hint
            // 2. Split on ';'
            // 3. Return each value in turn


            //if (obj.Hints.ContainsKey("ValidValues"))
            //{
                //Hint h = new Hint("out", null);
                //bool wtf = obj.Hints.TryGetValue("ValidValues", h);

                //for (int i = 0; i < obj.Hints.Count; ++i)
                //{

                //}
            //}

            max = values.Length;
        }

        // NEXT
        //
        public override void next()
        {
            pos++;
            if (pos >= values.Length)
            {
                pos = values.Length - 1;
                throw new MutatorCompleted();
            }
        }

        // COUNT
        //
        public override int count
        {
            get { return values.Length; }
        }

        // SUPPORTED
        //
        public new static bool supportedDataElement(DataElement obj)
        {
            if ((obj is Dom.String || obj is Dom.Number) && obj.isMutable)
            {
                if (obj.Hints.ContainsKey("ValidValues"))
                    return true;
            }

            return false;
        }

        // SEQUENCIAL_MUTATION
        //
        public override void sequencialMutation(Dom.DataElement obj)
        {
            //obj.MutatedValue = new Variant(values[pos]);
        }

        // RANDOM_MUTATION
        //
        public override void randomMutation(Dom.DataElement obj)
        {
            //obj.MutatedValue = new Variant(context.random.Choice<string>(values));
        }
	}
}
