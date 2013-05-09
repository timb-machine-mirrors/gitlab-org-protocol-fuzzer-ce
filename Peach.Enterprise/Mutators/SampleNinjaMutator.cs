
////
//// Copyright (c) Michael Eddington
////
//// Permission is hereby granted, free of charge, to any person obtaining a copy 
//// of this software and associated documentation files (the "Software"), to deal
//// in the Software without restriction, including without limitation the rights 
//// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
//// copies of the Software, and to permit persons to whom the Software is 
//// furnished to do so, subject to the following conditions:
////
//// The above copyright notice and this permission notice shall be included in	
//// all copies or substantial portions of the Software.
////
//// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
//// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
//// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
//// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
//// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//// SOFTWARE.
////

//// Authors:
////   Michael Eddington (mike@dejavusecurity.com)

//// $Id$

//using System;
//using System.Collections.Generic;
//using System.Text;
//using System.Data.Sql;
//using System.Data.SQLite;
//using System.Data.SqlClient;
//using System.Security.Cryptography;

//using Peach.Core.Dom;
//using Peach.Core;

//using NLog;

//namespace Peach.Enterprise.Mutators
//{
//    [Mutator("Will use existing samples to generate mutated files.")]
//    public class SampleNinjaMutator : Mutator
//    {
//        int _count = 0;

//        public SampleNinjaMutator(DataElement obj)
//            : base(obj)
//        {
//            name = "SampleNinja";


//            using (var Connection = new SQLiteConnection("data source=default.db"))
//            {
//                Connection.Open();

//                // Get the total number of elements we can generate for this data element.
//                using (var cmd = new SQLiteCommand(Connection))
//                {
//                    cmd.CommandText = @"
//select from count('x') 
//	from definition d, sample s, samplelement se, element e
//	where d.Name = ?
//	and e.name = ?
//	and s.definitionid = d.definitionid
//	and se.sampleid = s.sampleid
//	and se.elementid = e.elementid
//";

//                    cmd.Parameters.Add(new SQLiteParameter(System.Data.DbType.String));
//                    cmd.Parameters.Add(new SQLiteParameter(System.Data.DbType.String));
//                    cmd.Parameters[0].Value = ((Test)((DataModel)obj.getRoot()).action.parent.parent.parent)..parent). pitfile;
//                    cmd.Parameters[1].Value = obj.fullName;

//                    _count = (int)cmd.ExecuteScalar();
//                }
//            }
//        }

//        public override uint mutation
//        {
//            get { return pos; }
//            set { pos = value; }
//        }

//        public override int count
//        {
//            get { return _count; }
//        }

//        public new static bool supportedDataElement(DataElement obj)
//        {
//            if (obj.isMutable)
//                return true;

//            return false;
//        }

//        private void getRange(int size, out int start, out int end, int delta = int.MaxValue)
//        {
//            start = context.Random.Next(size);
//            end = context.Random.Next(size);

//            if (start > end)
//            {
//                int temp = end;
//                end = start;
//                start = temp;
//            }

//            if ((end - start) > delta)
//                end = start + delta;
//        }

//        public override void sequentialMutation(DataElement obj)
//        {
//            // The sequential logic relies on expand being thte 1st change function when we have generate functions
//            System.Diagnostics.Debug.Assert(generateFcns.Count == 0 || changeFcns[0] == changeExpandBuffer);

//            if (pos < generateFcns.Count)
//                obj.MutatedValue = new Variant(changeExpandBuffer(obj, generateFcns[(int)pos]));
//            else if (generateFcns.Count > 0)
//                obj.MutatedValue = new Variant(changeFcns[(int)pos - generateFcns.Count + 1](obj));
//            else
//                obj.MutatedValue = new Variant(changeFcns[(int)pos](obj));

//            obj.mutationFlags = DataElement.MUTATE_DEFAULT;
//            obj.mutationFlags |= DataElement.MUTATE_OVERRIDE_TYPE_TRANSFORM;
//        }

//        // RANDOM_MUTAION
//        //
//        public override void randomMutation(DataElement obj)
//        {
//            obj.MutatedValue = new Variant(context.Random.Choice(changeFcns)(obj));

//            obj.mutationFlags = DataElement.MUTATE_DEFAULT;
//            obj.mutationFlags |= DataElement.MUTATE_OVERRIDE_TYPE_TRANSFORM;
//        }

//        // EXPAND_BUFFER
//        //
//        private byte[] changeExpandBuffer(DataElement obj)
//        {
//            var how = context.Random.Choice(generateFcns);
//            return changeExpandBuffer(obj, how);
//        }

//        private byte[] changeExpandBuffer(DataElement obj, generateFcn generate)
//        {
//            // expand the size of our buffer
//            var data = obj.Value.Value;
//            int size = context.Random.Next(256);
//            int pos = context.Random.Next(data.Length);

//            return generate(data, pos, size);
//        }

//        // REDUCE_BUFFER
//        //
//        private byte[] changeReduceBuffer(DataElement obj)
//        {
//            // reduce the size of our buffer

//            var data = obj.Value.Value;
//            int start = 0;
//            int end = 0;

//            getRange(data.Length, out start, out end);

//            byte[] ret = new byte[data.Length - (end - start)];
//            Buffer.BlockCopy(data, 0, ret, 0, start);
//            Buffer.BlockCopy(data, end, ret, start, data.Length - end);

//            return ret;
//        }

//        // CHANGE_RANGE
//        //
//        private byte[] changeChangeRange(DataElement obj)
//        {
//            // change a sequence of bytes in our buffer

//            var data = obj.Value.Value;
//            int start = 0;
//            int end = 0;

//            getRange(data.Length, out start, out end, 100);

//            byte[] ret = new byte[data.Length];
//            Buffer.BlockCopy(data, 0, ret, 0, data.Length);

//            for (int i = start; i < end; ++i)
//                ret[i] = (byte)(context.Random.Next(256));

//            return ret;
//        }

//        // CHANGE_RANGE_SPECIAL
//        //
//        private byte[] changeRangeSpecial(DataElement obj)
//        {
//            // change a sequence of bytes in our buffer to some special chars

//            var data = obj.Value.Value;
//            int start = 0;
//            int end = 0;
//            byte[] special = { 0x00, 0x01, 0xFE, 0xFF };

//            getRange(data.Length, out start, out end, 100);

//            byte[] ret = new byte[data.Length];
//            Buffer.BlockCopy(data, 0, ret, 0, data.Length);

//            for (int i = start; i < end; ++i)
//                ret[i] = context.Random.Choice(special);

//            return ret;
//        }

//        // NULL_RANGE
//        //
//        private byte[] changeNullRange(DataElement obj)
//        {
//            // change a range of bytes to null

//            var data = obj.Value.Value;
//            int start = 0;
//            int end = 0;

//            getRange(data.Length, out start, out end, 100);

//            byte[] ret = new byte[data.Length];
//            Buffer.BlockCopy(data, 0, ret, 0, data.Length);

//            for (int i = start; i < end; ++i)
//                ret[i] = 0;

//            return ret;
//        }

//        // UNNULL_RANGE
//        //
//        private byte[] changeUnNullRange(DataElement obj)
//        {
//            // change all zeros in a range to something else

//            var data = obj.Value.Value;
//            int start = 0;
//            int end = 0;

//            getRange(data.Length, out start, out end, 100);

//            byte[] ret = new byte[data.Length];
//            Buffer.BlockCopy(data, 0, ret, 0, data.Length);

//            for (int i = start; i < end; ++i)
//                if (ret[i] == 0)
//                    ret[i] = (byte)(context.Random.Next(1, 256));

//            return ret;
//        }

//        // NEW_BYTES_SINGLE_RANDOM
//        //
//        private byte[] generateNewBytesSingleRandom(byte[] buf, int index, int size)
//        {
//            // Grow buffer by size bytes starting at index, each byte is the same random number
//            byte[] ret = new byte[buf.Length + size];
//            Buffer.BlockCopy(buf, 0, ret, 0, index);
//            Buffer.BlockCopy(buf, index, ret, index + size, buf.Length - index);

//            byte val = (byte)(context.Random.Next(256));

//            for (int i = index; i < index + size; ++i)
//                ret[i] = val;

//            return ret;
//        }

//        // NEW_BYTES_INCREMENTING
//        //
//        private byte[] generateNewBytesIncrementing(byte[] buf, int index, int size)
//        {
//            // Pick a starting value between [0, size] and grow buffer by
//            // a max of size bytes of incrementing values from [value,255]
//            System.Diagnostics.Debug.Assert(size < 256);

//            int val = context.Random.Next(size + 1);
//            int max = 256 - val;
//            if (size > max)
//                size = max;

//            byte[] ret = new byte[buf.Length + size];
//            Buffer.BlockCopy(buf, 0, ret, 0, index);
//            Buffer.BlockCopy(buf, index, ret, index + size, buf.Length - index);

//            for (int i = 0; i < size; ++i)
//                ret[index + i] = (byte)(val + i);

//            return ret;
//        }

//        // NEW_BYTES_ZERO
//        //
//        private byte[] generateNewBytesZero(byte[] buf, int index, int size)
//        {
//            // Grow buffer by size bytes starting at index, each byte is zero (NULL)
//            byte[] ret = new byte[buf.Length + size];
//            Buffer.BlockCopy(buf, 0, ret, 0, index);
//            Buffer.BlockCopy(buf, index, ret, index + size, buf.Length - index);

//            for (int i = index; i < index + size; ++i)
//                ret[i] = 0;

//            return ret;
//        }

//        // NEW_BYTES_ALL_RANDOM
//        //
//        private byte[] generateNewBytesAllRandom(byte[] buf, int index, int size)
//        {
//            // Grow buffer by size bytes starting at index, each byte is randomly generated
//            byte[] ret = new byte[buf.Length + size];
//            Buffer.BlockCopy(buf, 0, ret, 0, index);
//            Buffer.BlockCopy(buf, index, ret, index + size, buf.Length - index);

//            for (int i = index; i < index + size; ++i)
//                ret[i] = (byte)(context.Random.Next(256));

//            return ret;
//        }
//    }
//}

//// end
