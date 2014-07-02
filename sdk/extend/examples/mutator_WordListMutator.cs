
using System;
using System.Collections.Generic;
using System.Text;
using Peach.Core.Dom;

namespace Peach.Core.Mutators
{
    [Mutator("WordListMutator")]
    [Description("Allows a word list of different valid values to be specified")]
    [Hint("WordList", "Wordlist Containing newline seperated valid strings.")]
    public class WordListMutator : Mutator
    {
        // members
        //
        uint pos = 0;
        string[] values = new string[] { };

        // CTOR
        //
        public WordListMutator(DataElement obj)
        {
            pos = 0;
            name = "WordListMutator";
            generateValues(obj);
        }

        // GENERATE VALUES
        //
        public void generateValues(DataElement obj)
        {
            // 1. Get filename in hint
            // 2. Run function to add values in filename to list

            Hint h = null;
            if (obj.Hints.TryGetValue("WordList", out h))
            {
                AddListToValues(h.Value);                
            }
        }

        private void AddListToValues(string curfile)
        {
            var newvalues = new List<string>();
            if (System.IO.File.Exists(curfile))
            {
                newvalues.AddRange(System.IO.File.ReadAllLines(curfile));
            }
            else
            {
                throw new PeachException("Invalid Wordlist File: " + curfile);
            }
            newvalues.AddRange(values);
            values = newvalues.ToArray();
        }

        public override uint mutation
        {
            get { return pos; }
            set { pos = value; }
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
            if ((obj is Dom.String || obj is Dom.Number || obj is Dom.Blob) && obj.isMutable)
            {
                if (obj.Hints.ContainsKey("WordList"))
                    return true;
            }

            return false;
        }

        // SEQUENTIAL_MUTATION
        //
        public override void sequentialMutation(DataElement obj)
        {
            obj.MutatedValue = new Variant(values[pos]);
            obj.mutationFlags = MutateOverride.Default;
        }

        // RANDOM_MUTATION
        //
        public override void randomMutation(DataElement obj)
        {
            obj.MutatedValue = new Variant(context.Random.Choice(values));
            obj.mutationFlags = MutateOverride.Default;
        }
    }
}

// end
