﻿
/* Copyright (c) 2009 Michael Eddington
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy 
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights 
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
 * copies of the Software, and to permit persons to whom the Software is 
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in	
 * all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 * 
 * Authors:
 *   Michael Eddington (mike@dejavusecurity.com)
 * 
 * $Id$
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;
using System.Diagnostics;

using Peach.Enterprise.Language.DotNet.Generators;

namespace Peach.Enterprise.Language.DotNet
{
	/// <summary>
	/// This assembly fuzzer will use reflection to 
	/// locate all types and try to dynamically create
	/// and invoke them.
	/// 
	/// Methods and properties with type parameters will get
	/// fuzzed using data from PeachData.
	/// </summary>
	public class ObjectFuzzer : IContext
	{
		List<object> _objects = new List<object>();
		List<Type> _typeGenerators = new List<Type>();

		public ObjectFuzzer()
		{
			RegisterTypeGenerator(typeof(ArrayGenerator));
			RegisterTypeGenerator(typeof(GuidGenerator));
			RegisterTypeGenerator(typeof(BoolGenerator));
			RegisterTypeGenerator(typeof(NumberGenerator));
			RegisterTypeGenerator(typeof(CtorGenerator));
			RegisterTypeGenerator(typeof(MethodGenerator));
			RegisterTypeGenerator(typeof(PropertyGenerator));

			RegisterTypeGenerator(typeof(StringGenerator));

			// Always be last
			RegisterTypeGenerator(typeof(ClassGenerator));
		}

		public void RegisterTypeGenerator(Type typeGenerator)
		{
			if (typeGenerator.GetInterface("ITypeGenerator") != typeof(ITypeGenerator))
				throw new Exception("Attempted to register type generator that did not implement ITypeGenerator");

			_typeGenerators.Add(typeGenerator);
		}

		public void AddObject(object obj)
		{
			_objects.Add(obj);
		}

		public void Run()
		{
			uint count;
			Type type;

            List<Type> knownTypes = new List<Type>();

            // Build a list of object types we have already seen.
            foreach (object o in _objects)
                if(!knownTypes.Contains(o.GetType()))
                    knownTypes.Add(o.GetType());

            List<object> objectsToFuzz = _objects;
            List<object> fuzzed = new List<object>();
            List<object> newObjects = null;

            while (newObjects == null || newObjects.Count == 0)
            {
                newObjects = new List<object>();

                foreach (object o in objectsToFuzz)
                {
                    if (fuzzed.Contains(o))
                        continue;

                    fuzzed.Add(o);

                    count = 0;
                    type = o.GetType();
                    ObjectGenerator gen = (ObjectGenerator)ObjectGenerator.CreateInstance(this, null, o.GetType(), null);
                    gen.ObjectInstance = o;

                    try
                    {
                        while (true)
                        {
                            object obj = gen.GetValue();
                            if (obj != null && !fuzzed.Contains(obj) && !objectsToFuzz.Contains(obj) &&
                                !knownTypes.Contains(o.GetType()))
                            {
                                Console.Out.WriteLine("Found new object of type: " + obj.GetType().FullName);
                                newObjects.Add(obj);
                            }

                            gen.Next();
                            count++;
                        }
                    }
                    catch (GeneratorCompleted)
                    {
                        Debug.WriteLine(String.Format("Performed {0} tests on type {1}.", count, type.ToString()));
                    }
                }

                objectsToFuzz = newObjects;
            }
		}

		#region IContext Members

		public ITypeGenerator GetTypeGenerator(IGroup group, Type type, object[] obj)
		{
			Boolean ret;
			object[] parms = { type };
			object[] ctorArgs = { this, group, type, obj };

			foreach (Type typeGenerator in _typeGenerators)
			{
				try
				{
					ret = (Boolean)typeGenerator.InvokeMember("SupportedType",
						BindingFlags.Default | BindingFlags.InvokeMethod, null, null, parms);
				}
				catch (Exception e)
				{
					Debug.WriteLine("GetTypeGenerator(): SupportedType call excepted.");
					Debug.WriteLine(e.ToString());
					continue;
				}

				if (ret)
				{
					try
					{
						return (ITypeGenerator)typeGenerator.InvokeMember("CreateInstance",
							BindingFlags.Default | BindingFlags.InvokeMethod, null, null, ctorArgs);
					}
					catch (Exception e)
					{
						Debug.WriteLine("GetTypeGenerator(): CreateInstance call excepted.");
						Debug.WriteLine(e.ToString());
						continue;
					}
				}
			}

			return NullGenerator.CreateInstance(this, null, null, null);
		}

		#endregion
	}
}

// end
