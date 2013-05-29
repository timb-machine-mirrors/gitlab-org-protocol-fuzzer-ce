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
using System.Text;
using Newtonsoft.Json;
using Peach.Core.Dom;

namespace Peach.Core.Runtime
{
    public class WebWatcher : Watcher
    {
        public WebWatcher()
        {
        }

        protected override void MutationStrategy_Mutating(string elementName, string mutatorName)
        {
						//TODO use this to hightligh where in the model is being fuzzed 
        }

        protected override void Engine_IterationStarting(RunContext context, uint currentIteration,
                                                         uint? totalIterations)
        {
						try
            {
							if (context.controlIteration || context.controlRecordingIteration)
									return;

							StringBuilder stringBuilder = new StringBuilder();
							StringWriter stringWriter = new StringWriter(stringBuilder);

							using (JsonWriter jsonWriter = new JsonTextWriter(stringWriter))
							{
									jsonWriter.WriteStartObject();
									jsonWriter.WritePropertyName("DataModels");
									jsonWriter.WriteStartArray();

									foreach(var stateModel in context.dom.stateModels)
									foreach(var action in stateModel.Value.dataActions )
									{
										jsonWriter.WriteStartObject();
										DataModelToJson(action.dataModel.name, action.dataModel, jsonWriter);
										jsonWriter.WriteEndObject();
									}

									jsonWriter.WriteEndArray();

									jsonWriter.WritePropertyName("StateModels");
									jsonWriter.WriteValue("");

									jsonWriter.WritePropertyName("Agents");
									jsonWriter.WriteValue("");

									jsonWriter.WritePropertyName("Tests");
									jsonWriter.WriteValue("");

									jsonWriter.WriteEndObject();
							}

                FileStream jsonModel = File.OpenWrite("peach.json");
                StreamWriter streamWriter = new StreamWriter(jsonModel);
                streamWriter.Write(stringBuilder.ToString());
                streamWriter.Flush();
                streamWriter.Close();
                jsonModel.Close();
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