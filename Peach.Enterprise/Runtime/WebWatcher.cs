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
//   Adam Cecchetti (adam@dejavusecurity.com)

// $Id$

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Peach.Core.Dom;

namespace Peach.Core.Runtime
{
    public class WebWatcher : Watcher, IDisposable
    {
        private HttpListener httpListener = null;
        private Thread _httpThread;
        private string lastJson = ""; 

		public static string startUpPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + Path.DirectorySeparatorChar + "PeachView" + Path.DirectorySeparatorChar;
        private string _elementName = "";
        private string _mutatorName = "";
        private uint _totalIterations = 0; 


        public WebWatcher()
        {
		    if(!HttpListener.IsSupported)
			{
			    throw new PeachException("Web Watcher not supported on this platform!");
			}

            try
            {
                httpListener = new HttpListener();
                httpListener.Prefixes.Add("http://+:8888/");
                httpListener.Start();
                _httpThread = new Thread(new ThreadStart(ClientListener));
                _httpThread.Start();
            }
            catch(Exception e )
            {
                throw new PeachException("Could not start web watcher: are you admin?", e);
            }
        }

		public void ClientListener()
        {
            while (true)
            {
                try
                {
					HttpListenerContext context = httpListener.GetContext();
                    string filename = Path.GetFileName(context.Request.RawUrl);
                    string path = Path.Combine(startUpPath, filename);
                    byte[] msg;
                    if (!File.Exists(path)) 
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                        msg = File.ReadAllBytes(startUpPath + @"error.html");
                    }
                    else
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.OK;

                        msg = File.ReadAllBytes(path);
                    }

                    context.Response.ContentLength64 = msg.Length;
					if(path.Contains(".json"))
					{
					    context.Response.ContentType = "application/json"; 
					}

                    using (Stream s = context.Response.OutputStream)
                        s.Write(msg, 0, msg.Length);
                }
                catch( Exception)
                {
                    // swallow exceptions for now
                    // TODO: fix race condition for file r/w from http server / peach 
                    Thread.Sleep(500);
                }
            }
        }

		public void Dispose()
		{
		    httpListener.Close(); 
		}

        protected override void MutationStrategy_Mutating(string elementName, string mutatorName)
        {
            _elementName = elementName;
            _mutatorName = mutatorName; 
        }

        protected override void Engine_IterationStarting(RunContext context, uint currentIteration, uint? totalIterations)
        {
            if(totalIterations != null)
                _totalIterations = (uint)totalIterations; 
        }

        protected override void Engine_IterationFinished(RunContext context, uint currentIteration)
        {
            try
            {
                if (context.controlIteration || context.controlRecordingIteration)
                    return;

                StringBuilder stringBuilder = new StringBuilder();
                StringWriter stringWriter = new StringWriter(stringBuilder);

                using (JsonWriter jsonWriter = new JsonTextWriter(stringWriter))
                {
                    jsonWriter.WriteStartArray();
                    jsonWriter.WriteStartObject();
                    jsonWriter.WritePropertyName("IterationNumber");
                    jsonWriter.WriteValue(Convert.ToString(currentIteration));
                    jsonWriter.WritePropertyName("TotalIteration");
                    jsonWriter.WriteValue(Convert.ToString(_totalIterations));
                    jsonWriter.WritePropertyName("ElementName");
                    jsonWriter.WriteValue(_elementName);
                    jsonWriter.WritePropertyName("MutatorName");
                    jsonWriter.WriteValue(_mutatorName);
                    jsonWriter.WriteEndObject();

                    jsonWriter.WriteStartObject();
                    jsonWriter.WritePropertyName("DataModels");
                    jsonWriter.WriteStartArray();

                    foreach (var stateModel in context.dom.stateModels)
                    {
                        foreach (var action in stateModel.Value.dataActions)
                        {
                            jsonWriter.WriteStartObject();
                            DataModelToJson(action.dataModel.name, action.dataModel, jsonWriter);
                            jsonWriter.WriteEndObject();
                        }
                    }

                    jsonWriter.WriteEndArray();
                    jsonWriter.WriteEndObject();
                    jsonWriter.WriteEndArray();
                }

                lastJson = stringBuilder.ToString();
                if (File.Exists(startUpPath + "peach.json"))
                {
                    File.Delete(startUpPath + "peach.json");
                }

                try
                {
                    FileStream jsonFile = File.OpenWrite(startUpPath + "peach.json");
                    StreamWriter streamWriter = new StreamWriter(jsonFile);
                    streamWriter.Write(lastJson);
                    streamWriter.Flush();
                    streamWriter.Close();
                    jsonFile.Close();
                }
                catch (Exception)
                {
                }
                //Thread.Sleep(1000);
            }
			catch(Exception e)
			{
			    throw new PeachException("Failure writing Peach JSON Model for WebWatcher",e);
			}
        }

		private void DataModelToJson(string name, DataElementContainer model, JsonWriter writer)
		{
            writer.WritePropertyName("name");
            writer.WriteValue(name);
            writer.WritePropertyName("children");
            writer.WriteStartArray();
            foreach(var item in model)
            {
                writer.WriteStartObject();

                if(item is Dom.Array)
                {
                    DataModelToJson(item.name, (DataElementContainer)item, writer);							    
                }

                if(item is Block)
                {
                    DataModelToJson(item.name, (DataElementContainer)item, writer);
                }

                if(item is Dom.Flag)
                {
                    DataModelToJson(item.name, (DataElementContainer)item, writer); 
                }

                if(item is Dom.Choice)
                {
                    DataModelToJson(item.name, (DataElementContainer)item, writer);
                }

                if(item is Dom.String)
                {
                    writer.WritePropertyName("name");
                    writer.WriteValue(item.name); 
                    writer.WritePropertyName("type");
                    writer.WriteValue("String");

                }

                if(item is Number) 
                {
                    writer.WritePropertyName("name");
                    writer.WriteValue(item.name); 
                    writer.WritePropertyName("type");
                    writer.WriteValue("Number");

                }

                if(item is Blob) 
                {
                    writer.WritePropertyName("name");
                    writer.WriteValue(item.name);
                    writer.WritePropertyName("type");
                    writer.WriteValue("Number");
                }

                if(item is XmlElement) 
                {
                    writer.WritePropertyName("name");
                    writer.WriteValue(item.name);
                    writer.WritePropertyName("type");
                    writer.WriteValue("XmlElement");
                }
                
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
		}

		private string StateModelToJson(StateModel model)
		{
		    return "StateModel";
		}
		
	    private string AgentToJson(Dom.Agent agent)
	    {
	        return "Agent";
		}
    }
}

