
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
using System.Collections;
using System.Reflection;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using System.Runtime.Serialization.Formatters;
using System.Threading;
using Peach.Core;
using Peach.Pro.OS.Windows.Publishers.Com;

namespace Peach.Pro.ComContainer
{
	public class Program
	{
		public static void Main(string[] args)
		{
			var asm = Assembly.GetExecutingAssembly();

			Console.WriteLine("> Peach Com Container v{0}", asm.GetName().Version);
			Console.WriteLine("> {0}", asm.GetCopyright());
			Console.WriteLine();

			var provider = new BinaryServerFormatterSinkProvider
			{
				TypeFilterLevel = TypeFilterLevel.Full
			};

			var props = new Hashtable();
			props["name"] = "ipc";
			props["portName"] = "Peach_Com_Container";

			var channel = new IpcChannel(props, null, provider);

			ChannelServices.RegisterChannel(channel, false);

			try
			{
				RemotingConfiguration.RegisterWellKnownServiceType(
					typeof(ComContainerServer),
					"PeachComContainer",
					WellKnownObjectMode.Singleton);

				Thread.Sleep(Timeout.Infinite);
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}
			finally
			{
				ChannelServices.UnregisterChannel(channel);
			}
		}
	}
}
