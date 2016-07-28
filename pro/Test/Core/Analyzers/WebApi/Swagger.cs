using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using NUnit.Framework;
using Peach.Core.Test;
using Peach.Pro.Core.Analyzers.WebApi;

namespace Peach.Pro.Test.Core.Analyzers.WebApi
{
	public class Swagger
	{
		[Test]
		[Peach]
		[Quick]
		public void TestSwaggerToWebApi()
		{
			var apiEndPoint = SwaggerToWebApi.Convert(swagger2_PeachApi);

			var apiCollection = new WebApiCollection();
			apiCollection.EndPoints.Add(apiEndPoint);

			Assert.AreEqual(1, apiCollection.EndPoints.Count);
			Assert.AreEqual(29, apiCollection.EndPoints[0].Paths.Count);
			Assert.AreEqual(2, apiCollection.EndPoints[0].Paths[0].Operations.Count);
			Assert.AreEqual(2, apiCollection.EndPoints[0].Paths[1].Operations.Count);
			Assert.AreEqual(1, apiCollection.EndPoints[0].Paths[2].Operations.Count);
			Assert.AreEqual(1, apiCollection.EndPoints[0].Paths[3].Operations.Count);
		}

		[Test]
		[Peach]
		[Quick]
		public void TestWebApiToDom()
		{
			var apiEndPoint = SwaggerToWebApi.Convert(swagger2_PeachApi);

			var apiCollection = new WebApiCollection();
			apiCollection.EndPoints.Add(apiEndPoint);

			var dom = new Peach.Core.Dom.Dom();
			WebApiToDom.Convert(dom, apiCollection);

			foreach (var action in dom.tests[0].stateModel.states.SelectMany(state => state.actions))
			{
				var call = action as Peach.Core.Dom.Actions.Call;
				if (call == null)
					continue;

				Assert.AreEqual(-1, call.method.IndexOf("{id}"));
			}

			var settings = new XmlWriterSettings
			{
				Encoding = Encoding.UTF8,
				Indent = true
			};

			Assert.DoesNotThrow(() =>
			{
				using (var sout = new MemoryStream())
				{
					using (var xml = XmlWriter.Create(sout, settings))
					{
						xml.WriteStartDocument();
						dom.WritePit(xml);
						xml.WriteEndDocument();
					}
				}
			});
		}

		[Test]
		[Peach]
		[Quick]
		public void TestWebApiToDom_PetStore()
		{
			var apiEndPoint = SwaggerToWebApi.Convert(swagger2_PetStore);

			var apiCollection = new WebApiCollection();
			apiCollection.EndPoints.Add(apiEndPoint);

			var dom = new Peach.Core.Dom.Dom();
			WebApiToDom.Convert(dom, apiCollection);

			foreach (var action in dom.tests[0].stateModel.states.SelectMany(state => state.actions))
			{
				var call = action as Peach.Core.Dom.Actions.Call;
				if (call == null)
					continue;

				Assert.AreEqual(-1, call.method.IndexOf("{id}"));
			}

			var settings = new XmlWriterSettings
			{
				Encoding = Encoding.UTF8,
				Indent = true
			};

			Assert.DoesNotThrow(() => {
				using (var sout = new MemoryStream())
				{
					using (var xml = XmlWriter.Create(sout, settings))
					{
						xml.WriteStartDocument();
						dom.WritePit(xml);
						xml.WriteEndDocument();
					}
				}
			});
		}

		/// <summary>
		/// Swaggers v2.0 public example
		/// </summary>
		private string swagger2_PetStore = @"{
  ""swagger"": ""2.0"",
  ""info"": {
    ""version"": ""1.0.0"",
    ""title"": ""Swagger Petstore"",
    ""description"": ""A sample API that uses a petstore as an example to demonstrate features in the swagger-2.0 specification"",
    ""termsOfService"": ""http://swagger.io/terms/"",
    ""contact"": {
      ""name"": ""Swagger API Team""
    },
    ""license"": {
      ""name"": ""MIT""
    }
  },
  ""host"": ""petstore.swagger.io"",
  ""basePath"": ""/api"",
  ""schemes"": [
    ""http""
  ],
  ""consumes"": [
    ""application/json""
  ],
  ""produces"": [
    ""application/json""
  ],
  ""paths"": {
    ""/pets"": {
      ""get"": {
        ""description"": ""Returns all pets from the system that the user has access to"",
        ""operationId"": ""findPets"",
        ""produces"": [
          ""application/json"",
          ""application/xml"",
          ""text/xml"",
          ""text/html""
        ],
        ""parameters"": [
          {
            ""name"": ""tags"",
            ""in"": ""query"",
            ""description"": ""tags to filter by"",
            ""required"": false,
            ""type"": ""array"",
            ""items"": {
              ""type"": ""string""
            },
            ""collectionFormat"": ""csv""
          },
          {
            ""name"": ""limit"",
            ""in"": ""query"",
            ""description"": ""maximum number of results to return"",
            ""required"": false,
            ""type"": ""integer"",
            ""format"": ""int32""
          }
        ],
        ""responses"": {
          ""200"": {
            ""description"": ""pet response"",
            ""schema"": {
              ""type"": ""array"",
              ""items"": {
                ""$ref"": ""#/definitions/Pet""
              }
            }
          },
          ""default"": {
            ""description"": ""unexpected error"",
            ""schema"": {
              ""$ref"": ""#/definitions/ErrorModel""
            }
          }
        }
      },
      ""post"": {
        ""description"": ""Creates a new pet in the store.  Duplicates are allowed"",
        ""operationId"": ""addPet"",
        ""produces"": [
          ""application/json""
        ],
        ""parameters"": [
          {
            ""name"": ""pet"",
            ""in"": ""body"",
            ""description"": ""Pet to add to the store"",
            ""required"": true,
            ""schema"": {
              ""$ref"": ""#/definitions/NewPet""
            }
          }
        ],
        ""responses"": {
          ""200"": {
            ""description"": ""pet response"",
            ""schema"": {
              ""$ref"": ""#/definitions/Pet""
            }
          },
          ""default"": {
            ""description"": ""unexpected error"",
            ""schema"": {
              ""$ref"": ""#/definitions/ErrorModel""
            }
          }
        }
      }
    },
    ""/pets/{id}"": {
      ""get"": {
        ""description"": ""Returns a user based on a single ID, if the user does not have access to the pet"",
        ""operationId"": ""findPetById"",
        ""produces"": [
          ""application/json"",
          ""application/xml"",
          ""text/xml"",
          ""text/html""
        ],
        ""parameters"": [
          {
            ""name"": ""id"",
            ""in"": ""path"",
            ""description"": ""ID of pet to fetch"",
            ""required"": true,
            ""type"": ""integer"",
            ""format"": ""int64""
          }
        ],
        ""responses"": {
          ""200"": {
            ""description"": ""pet response"",
            ""schema"": {
              ""$ref"": ""#/definitions/Pet""
            }
          },
          ""default"": {
            ""description"": ""unexpected error"",
            ""schema"": {
              ""$ref"": ""#/definitions/ErrorModel""
            }
          }
        }
      },
      ""delete"": {
        ""description"": ""deletes a single pet based on the ID supplied"",
        ""operationId"": ""deletePet"",
        ""parameters"": [
          {
            ""name"": ""id"",
            ""in"": ""path"",
            ""description"": ""ID of pet to delete"",
            ""required"": true,
            ""type"": ""integer"",
            ""format"": ""int64""
          }
        ],
        ""responses"": {
          ""204"": {
            ""description"": ""pet deleted""
          },
          ""default"": {
            ""description"": ""unexpected error"",
            ""schema"": {
              ""$ref"": ""#/definitions/ErrorModel""
            }
          }
        }
      }
    }
  },
  ""definitions"": {
    ""Pet"": {
      ""type"": ""object"",
      ""allOf"": [
        {
          ""$ref"": ""#/definitions/NewPet""
        },
        {
          ""required"": [
            ""id""
          ],
          ""properties"": {
            ""id"": {
              ""type"": ""integer"",
              ""format"": ""int64""
            }
          }
        }
      ]
    },
    ""NewPet"": {
      ""type"": ""object"",
      ""required"": [
        ""name""
      ],
      ""properties"": {
        ""name"": {
          ""type"": ""string""
        },
        ""tag"": {
          ""type"": ""string""
        }
      }
    },
    ""ErrorModel"": {
      ""type"": ""object"",
      ""required"": [
        ""code"",
        ""message""
      ],
      ""properties"": {
        ""code"": {
          ""type"": ""integer"",
          ""format"": ""int32""
        },
        ""message"": {
          ""type"": ""string""
        }
      }
    }
  }
}";
		/// <summary>
		/// Peach API Swagger
		/// </summary>
		string swagger2_PeachApi = @"{""swagger"":""2.0"",""info"":{""version"":""v1"",""title"":""Peach Fuzzer API"",""description"":""The REST API used for controlling the fuzzer."",""termsOfService"":""End User License Agreement"",""contact"":{""name"":""Peach Fuzzer"",""url"":""http://www.peachfuzzer.com/contact/"",""email"":""support@peachfuzzer.com""},""license"":{""name"":""EULA"",""url"":""http://www.peachfuzzer.com/contact/eula/""}},""host"":""192.168.1.115:8888"",""schemes"":[""http""],""paths"":{""/p/jobs"":{""get"":{""tags"":[""Jobs""],""summary"":""Gets the list of all jobs"",""description"":""Returns a list of all jobs in the database"",""operationId"":""Jobs_Get"",""consumes"":[],""produces"":[""application/json"",""text/json"",""application/xml"",""text/xml""],""parameters"":[{""name"":""dryrun"",""in"":""query"",""description"":""Include test runs"",""required"":false,""type"":""boolean""},{""name"":""running"",""in"":""query"",""description"":""Include currently running jobs"",""required"":false,""type"":""boolean""}],""responses"":{""200"":{""description"":""OK"",""schema"":{""type"":""array"",""items"":{""$ref"":""#/definitions/Job""}}},""500"":{""description"":""Internal Server Error"",""schema"":{""$ref"":""#/definitions/Error""}},""401"":{""description"":""EULA has not been accepted""},""402"":{""description"":""Invalid or expired license""}},""deprecated"":false},""post"":{""tags"":[""Jobs""],""summary"":""Create a new job"",""description"":""This is how you create a job"",""operationId"":""Jobs_Post"",""consumes"":[""application/json"",""text/json"",""application/xml"",""text/xml"",""application/x-www-form-urlencoded""],""produces"":[""application/json"",""text/json"",""application/xml"",""text/xml""],""parameters"":[{""name"":""request"",""in"":""body"",""description"":""Options for the job to create"",""required"":true,""schema"":{""$ref"":""#/definitions/JobRequest""}}],""responses"":{""200"":{""description"":""OK"",""schema"":{""$ref"":""#/definitions/Job""}},""400"":{""description"":""Invalid job request""},""403"":{""description"":""Unable to start the job""},""404"":{""description"":""Specified job does not exist""},""500"":{""description"":""Internal Server Error"",""schema"":{""$ref"":""#/definitions/Error""}},""401"":{""description"":""EULA has not been accepted""},""402"":{""description"":""Invalid or expired license""}},""deprecated"":false}},""/p/jobs/{id}"":{""get"":{""tags"":[""Jobs""],""operationId"":""Jobs_Get"",""consumes"":[],""produces"":[""application/json"",""text/json"",""application/xml"",""text/xml""],""parameters"":[{""name"":""id"",""in"":""path"",""required"":true,""type"":""string""}],""responses"":{""200"":{""description"":""OK"",""schema"":{""$ref"":""#/definitions/Job""}},""404"":{""description"":""Specified job does not exist""},""500"":{""description"":""Internal Server Error"",""schema"":{""$ref"":""#/definitions/Error""}},""401"":{""description"":""EULA has not been accepted""},""402"":{""description"":""Invalid or expired license""}},""deprecated"":false},""delete"":{""tags"":[""Jobs""],""operationId"":""Jobs_Delete"",""consumes"":[],""produces"":[""application/json"",""text/json"",""application/xml"",""text/xml""],""parameters"":[{""name"":""id"",""in"":""path"",""required"":true,""type"":""string""}],""responses"":{""200"":{""description"":""OK"",""schema"":{""$ref"":""#/definitions/Object""}},""404"":{""description"":""Specified job does not exist""},""500"":{""description"":""Internal Server Error"",""schema"":{""$ref"":""#/definitions/Error""}},""401"":{""description"":""EULA has not been accepted""},""402"":{""description"":""Invalid or expired license""}},""deprecated"":false}},""/p/jobs/{id}/nodes"":{""get"":{""tags"":[""Jobs""],""operationId"":""Jobs_GetNodes"",""consumes"":[],""produces"":[""application/json"",""text/json"",""application/xml"",""text/xml""],""parameters"":[{""name"":""id"",""in"":""path"",""required"":true,""type"":""string""}],""responses"":{""200"":{""description"":""OK"",""schema"":{""type"":""array"",""items"":{""type"":""string""}}},""404"":{""description"":""Specified job does not exist""},""500"":{""description"":""Internal Server Error"",""schema"":{""$ref"":""#/definitions/Error""}},""401"":{""description"":""EULA has not been accepted""},""402"":{""description"":""Invalid or expired license""}},""deprecated"":true}},""/p/jobs/{id}/nodes/first"":{""get"":{""tags"":[""Jobs""],""operationId"":""Jobs_GetNodesFirst"",""consumes"":[],""produces"":[""application/json"",""text/json"",""application/xml"",""text/xml""],""parameters"":[{""name"":""id"",""in"":""path"",""required"":true,""type"":""string""}],""responses"":{""200"":{""description"":""OK"",""schema"":{""$ref"":""#/definitions/TestResult""}},""404"":{""description"":""Specified job does not exist""},""500"":{""description"":""Internal Server Error"",""schema"":{""$ref"":""#/definitions/Error""}},""401"":{""description"":""EULA has not been accepted""},""402"":{""description"":""Invalid or expired license""}},""deprecated"":false}},""/p/jobs/{id}/nodes/{nodeId}/log"":{""get"":{""tags"":[""Jobs""],""operationId"":""Jobs_GetNodesLog"",""consumes"":[],""produces"":[""text/plain""],""parameters"":[{""name"":""id"",""in"":""path"",""required"":true,""type"":""string""},{""name"":""nodeId"",""in"":""path"",""required"":true,""type"":""string""}],""responses"":{""200"":{""description"":""OK"",""schema"":{""type"":""file""}},""404"":{""description"":""Specified job does not exist""},""500"":{""description"":""Internal Server Error"",""schema"":{""$ref"":""#/definitions/Error""}},""401"":{""description"":""EULA has not been accepted""},""402"":{""description"":""Invalid or expired license""}},""deprecated"":true}},""/p/jobs/{id}/faults"":{""get"":{""tags"":[""Jobs""],""operationId"":""Jobs_GetFaults"",""consumes"":[],""produces"":[""application/json"",""text/json"",""application/xml"",""text/xml""],""parameters"":[{""name"":""id"",""in"":""path"",""required"":true,""type"":""string""}],""responses"":{""200"":{""description"":""OK"",""schema"":{""type"":""array"",""items"":{""$ref"":""#/definitions/FaultSummary""}}},""404"":{""description"":""Specified job does not exist""},""500"":{""description"":""Internal Server Error"",""schema"":{""$ref"":""#/definitions/Error""}},""401"":{""description"":""EULA has not been accepted""},""402"":{""description"":""Invalid or expired license""}},""deprecated"":false}},""/p/jobs/{id}/faults/{faultId}"":{""get"":{""tags"":[""Jobs""],""operationId"":""Jobs_GetFault"",""consumes"":[],""produces"":[""application/json"",""text/json"",""application/xml"",""text/xml""],""parameters"":[{""name"":""id"",""in"":""path"",""required"":true,""type"":""string""},{""name"":""faultId"",""in"":""path"",""required"":true,""type"":""integer"",""format"":""int64""}],""responses"":{""200"":{""description"":""OK"",""schema"":{""$ref"":""#/definitions/Fault""}},""404"":{""description"":""Specified job or fault does not exist""},""500"":{""description"":""Internal Server Error"",""schema"":{""$ref"":""#/definitions/Error""}},""401"":{""description"":""EULA has not been accepted""},""402"":{""description"":""Invalid or expired license""}},""deprecated"":false}},""/p/jobs/{id}/faults/{faultId}/data/{fileId}"":{""get"":{""tags"":[""Jobs""],""operationId"":""Jobs_GetFaultFile"",""consumes"":[],""produces"":[""application/octet-stream""],""parameters"":[{""name"":""id"",""in"":""path"",""required"":true,""type"":""string""},{""name"":""faultId"",""in"":""path"",""required"":true,""type"":""integer"",""format"":""int64""},{""name"":""fileId"",""in"":""path"",""required"":true,""type"":""integer"",""format"":""int64""}],""responses"":{""200"":{""description"":""OK"",""schema"":{""type"":""file""}},""404"":{""description"":""Specified job, fault or file does not exist""},""500"":{""description"":""Internal Server Error"",""schema"":{""$ref"":""#/definitions/Error""}},""401"":{""description"":""EULA has not been accepted""},""402"":{""description"":""Invalid or expired license""}},""deprecated"":false}},""/p/jobs/{id}/faults/{faultId}/archive"":{""get"":{""tags"":[""Jobs""],""operationId"":""Jobs_GetFaultArchive"",""consumes"":[],""produces"":[""application/x-zip-compressed""],""parameters"":[{""name"":""id"",""in"":""path"",""required"":true,""type"":""string""},{""name"":""faultId"",""in"":""path"",""required"":true,""type"":""integer"",""format"":""int64""}],""responses"":{""200"":{""description"":""OK"",""schema"":{""type"":""file""}},""404"":{""description"":""Specified job or fault does not exist""},""500"":{""description"":""Internal Server Error"",""schema"":{""$ref"":""#/definitions/Error""}},""401"":{""description"":""EULA has not been accepted""},""402"":{""description"":""Invalid or expired license""}},""deprecated"":false}},""/p/jobs/{id}/report"":{""get"":{""tags"":[""Jobs""],""operationId"":""Jobs_GetReport"",""consumes"":[],""produces"":[""application/pdf""],""parameters"":[{""name"":""id"",""in"":""path"",""required"":true,""type"":""string""}],""responses"":{""200"":{""description"":""OK"",""schema"":{""type"":""file""}},""404"":{""description"":""Specified job does not exist""},""500"":{""description"":""Internal Server Error"",""schema"":{""$ref"":""#/definitions/Error""}},""401"":{""description"":""EULA has not been accepted""},""402"":{""description"":""Invalid or expired license""}},""deprecated"":false}},""/p/jobs/{id}/metrics/faultTimeline"":{""get"":{""tags"":[""Jobs""],""operationId"":""Jobs_GetFaultTimelineMetric"",""consumes"":[],""produces"":[""application/json"",""text/json"",""application/xml"",""text/xml""],""parameters"":[{""name"":""id"",""in"":""path"",""required"":true,""type"":""string""}],""responses"":{""200"":{""description"":""OK"",""schema"":{""type"":""array"",""items"":{""$ref"":""#/definitions/FaultTimelineMetric""}}},""404"":{""description"":""Specified job does not exist""},""500"":{""description"":""Internal Server Error"",""schema"":{""$ref"":""#/definitions/Error""}},""401"":{""description"":""EULA has not been accepted""},""402"":{""description"":""Invalid or expired license""}},""deprecated"":false}},""/p/jobs/{id}/metrics/bucketTimeline"":{""get"":{""tags"":[""Jobs""],""operationId"":""Jobs_GetBucketTimelineMetric"",""consumes"":[],""produces"":[""application/json"",""text/json"",""application/xml"",""text/xml""],""parameters"":[{""name"":""id"",""in"":""path"",""required"":true,""type"":""string""}],""responses"":{""200"":{""description"":""OK"",""schema"":{""type"":""array"",""items"":{""$ref"":""#/definitions/BucketTimelineMetric""}}},""404"":{""description"":""Specified job does not exist""},""500"":{""description"":""Internal Server Error"",""schema"":{""$ref"":""#/definitions/Error""}},""401"":{""description"":""EULA has not been accepted""},""402"":{""description"":""Invalid or expired license""}},""deprecated"":false}},""/p/jobs/{id}/metrics/mutators"":{""get"":{""tags"":[""Jobs""],""operationId"":""Jobs_GetMutatorMetric"",""consumes"":[],""produces"":[""application/json"",""text/json"",""application/xml"",""text/xml""],""parameters"":[{""name"":""id"",""in"":""path"",""required"":true,""type"":""string""}],""responses"":{""200"":{""description"":""OK"",""schema"":{""type"":""array"",""items"":{""$ref"":""#/definitions/MutatorMetric""}}},""404"":{""description"":""Specified job does not exist""},""500"":{""description"":""Internal Server Error"",""schema"":{""$ref"":""#/definitions/Error""}},""401"":{""description"":""EULA has not been accepted""},""402"":{""description"":""Invalid or expired license""}},""deprecated"":false}},""/p/jobs/{id}/metrics/elements"":{""get"":{""tags"":[""Jobs""],""operationId"":""Jobs_GetElementMetric"",""consumes"":[],""produces"":[""application/json"",""text/json"",""application/xml"",""text/xml""],""parameters"":[{""name"":""id"",""in"":""path"",""required"":true,""type"":""string""}],""responses"":{""200"":{""description"":""OK"",""schema"":{""type"":""array"",""items"":{""$ref"":""#/definitions/ElementMetric""}}},""404"":{""description"":""Specified job does not exist""},""500"":{""description"":""Internal Server Error"",""schema"":{""$ref"":""#/definitions/Error""}},""401"":{""description"":""EULA has not been accepted""},""402"":{""description"":""Invalid or expired license""}},""deprecated"":false}},""/p/jobs/{id}/metrics/states"":{""get"":{""tags"":[""Jobs""],""operationId"":""Jobs_GetStateMetric"",""consumes"":[],""produces"":[""application/json"",""text/json"",""application/xml"",""text/xml""],""parameters"":[{""name"":""id"",""in"":""path"",""required"":true,""type"":""string""}],""responses"":{""200"":{""description"":""OK"",""schema"":{""type"":""array"",""items"":{""$ref"":""#/definitions/StateMetric""}}},""404"":{""description"":""Specified job does not exist""},""500"":{""description"":""Internal Server Error"",""schema"":{""$ref"":""#/definitions/Error""}},""401"":{""description"":""EULA has not been accepted""},""402"":{""description"":""Invalid or expired license""}},""deprecated"":false}},""/p/jobs/{id}/metrics/dataset"":{""get"":{""tags"":[""Jobs""],""operationId"":""Jobs_GetDatasetMetric"",""consumes"":[],""produces"":[""application/json"",""text/json"",""application/xml"",""text/xml""],""parameters"":[{""name"":""id"",""in"":""path"",""required"":true,""type"":""string""}],""responses"":{""200"":{""description"":""OK"",""schema"":{""type"":""array"",""items"":{""$ref"":""#/definitions/DatasetMetric""}}},""404"":{""description"":""Specified job does not exist""},""500"":{""description"":""Internal Server Error"",""schema"":{""$ref"":""#/definitions/Error""}},""401"":{""description"":""EULA has not been accepted""},""402"":{""description"":""Invalid or expired license""}},""deprecated"":false}},""/p/jobs/{id}/metrics/buckets"":{""get"":{""tags"":[""Jobs""],""operationId"":""Jobs_GetBucketMetric"",""consumes"":[],""produces"":[""application/json"",""text/json"",""application/xml"",""text/xml""],""parameters"":[{""name"":""id"",""in"":""path"",""required"":true,""type"":""string""}],""responses"":{""200"":{""description"":""OK"",""schema"":{""type"":""array"",""items"":{""$ref"":""#/definitions/BucketMetric""}}},""404"":{""description"":""Specified job does not exist""},""500"":{""description"":""Internal Server Error"",""schema"":{""$ref"":""#/definitions/Error""}},""401"":{""description"":""EULA has not been accepted""},""402"":{""description"":""Invalid or expired license""}},""deprecated"":false}},""/p/jobs/{id}/metrics/iterations"":{""get"":{""tags"":[""Jobs""],""operationId"":""Jobs_GetIterationMetric"",""consumes"":[],""produces"":[""application/json"",""text/json"",""application/xml"",""text/xml""],""parameters"":[{""name"":""id"",""in"":""path"",""required"":true,""type"":""string""}],""responses"":{""200"":{""description"":""OK"",""schema"":{""type"":""array"",""items"":{""$ref"":""#/definitions/IterationMetric""}}},""404"":{""description"":""Specified job does not exist""},""500"":{""description"":""Internal Server Error"",""schema"":{""$ref"":""#/definitions/Error""}},""401"":{""description"":""EULA has not been accepted""},""402"":{""description"":""Invalid or expired license""}},""deprecated"":false}},""/p/jobs/{id}/pause"":{""get"":{""tags"":[""Jobs""],""operationId"":""Jobs_GetPauseJob"",""consumes"":[],""produces"":[""application/json"",""text/json"",""application/xml"",""text/xml""],""parameters"":[{""name"":""id"",""in"":""path"",""required"":true,""type"":""string""}],""responses"":{""200"":{""description"":""OK"",""schema"":{""$ref"":""#/definitions/Object""}},""403"":{""description"":""Job state doesn't allow operation""},""404"":{""description"":""Specified job does not exist""},""500"":{""description"":""Internal Server Error"",""schema"":{""$ref"":""#/definitions/Error""}},""401"":{""description"":""EULA has not been accepted""},""402"":{""description"":""Invalid or expired license""}},""deprecated"":false}},""/p/jobs/{id}/continue"":{""get"":{""tags"":[""Jobs""],""operationId"":""Jobs_GetContinueJob"",""consumes"":[],""produces"":[""application/json"",""text/json"",""application/xml"",""text/xml""],""parameters"":[{""name"":""id"",""in"":""path"",""required"":true,""type"":""string""}],""responses"":{""200"":{""description"":""OK"",""schema"":{""$ref"":""#/definitions/Object""}},""403"":{""description"":""Job state doesn't allow operation""},""404"":{""description"":""Specified job does not exist""},""500"":{""description"":""Internal Server Error"",""schema"":{""$ref"":""#/definitions/Error""}},""401"":{""description"":""EULA has not been accepted""},""402"":{""description"":""Invalid or expired license""}},""deprecated"":false}},""/p/jobs/{id}/stop"":{""get"":{""tags"":[""Jobs""],""operationId"":""Jobs_GetStopJob"",""consumes"":[],""produces"":[""application/json"",""text/json"",""application/xml"",""text/xml""],""parameters"":[{""name"":""id"",""in"":""path"",""required"":true,""type"":""string""}],""responses"":{""200"":{""description"":""OK"",""schema"":{""$ref"":""#/definitions/Object""}},""403"":{""description"":""Job state doesn't allow operation""},""404"":{""description"":""Specified job does not exist""},""500"":{""description"":""Internal Server Error"",""schema"":{""$ref"":""#/definitions/Error""}},""401"":{""description"":""EULA has not been accepted""},""402"":{""description"":""Invalid or expired license""}},""deprecated"":false}},""/p/jobs/{id}/kill"":{""get"":{""tags"":[""Jobs""],""operationId"":""Jobs_GetKillJob"",""consumes"":[],""produces"":[""application/json"",""text/json"",""application/xml"",""text/xml""],""parameters"":[{""name"":""id"",""in"":""path"",""required"":true,""type"":""string""}],""responses"":{""200"":{""description"":""OK"",""schema"":{""$ref"":""#/definitions/Object""}},""403"":{""description"":""Job state doesn't allow operation""},""404"":{""description"":""Specified job does not exist""},""500"":{""description"":""Internal Server Error"",""schema"":{""$ref"":""#/definitions/Error""}},""401"":{""description"":""EULA has not been accepted""},""402"":{""description"":""Invalid or expired license""}},""deprecated"":false}},""/p/libraries"":{""get"":{""tags"":[""Libraries""],""summary"":""Gets the list of all libraries"",""description"":""Returns a list of all libraries"",""operationId"":""Libraries_Get"",""consumes"":[],""produces"":[""application/json"",""text/json"",""application/xml"",""text/xml""],""responses"":{""200"":{""description"":""OK"",""schema"":{""type"":""array"",""items"":{""$ref"":""#/definitions/Library""}}},""500"":{""description"":""Internal Server Error"",""schema"":{""$ref"":""#/definitions/Error""}},""401"":{""description"":""EULA has not been accepted""},""402"":{""description"":""Invalid or expired license""}},""deprecated"":false}},""/p/libraries/{id}"":{""get"":{""tags"":[""Libraries""],""summary"":""Gets the details for specified library"",""description"":""The library details contains the list of all pits contained in the library"",""operationId"":""Libraries_Get"",""consumes"":[],""produces"":[""application/json"",""text/json"",""application/xml"",""text/xml""],""parameters"":[{""name"":""id"",""in"":""path"",""description"":""Library identifier"",""required"":true,""type"":""string""}],""responses"":{""200"":{""description"":""OK"",""schema"":{""$ref"":""#/definitions/Library""}},""404"":{""description"":""Specified library does not exist""},""500"":{""description"":""Internal Server Error"",""schema"":{""$ref"":""#/definitions/Error""}},""401"":{""description"":""EULA has not been accepted""},""402"":{""description"":""Invalid or expired license""}},""deprecated"":false}},""/p/license"":{""get"":{""tags"":[""License""],""operationId"":""License_Get"",""consumes"":[],""produces"":[""application/json"",""text/json"",""application/xml"",""text/xml""],""responses"":{""200"":{""description"":""OK"",""schema"":{""$ref"":""#/definitions/License""}},""500"":{""description"":""Internal Server Error"",""schema"":{""$ref"":""#/definitions/Error""}}},""deprecated"":false}},""/p/nodes"":{""get"":{""tags"":[""Nodes""],""operationId"":""Nodes_Get"",""consumes"":[],""produces"":[""application/json"",""text/json"",""application/xml"",""text/xml""],""responses"":{""200"":{""description"":""OK"",""schema"":{""type"":""array"",""items"":{""$ref"":""#/definitions/Node""}}},""500"":{""description"":""Internal Server Error"",""schema"":{""$ref"":""#/definitions/Error""}},""401"":{""description"":""EULA has not been accepted""},""402"":{""description"":""Invalid or expired license""}},""deprecated"":false}},""/p/nodes/{id}"":{""get"":{""tags"":[""Nodes""],""operationId"":""Nodes_Get"",""consumes"":[],""produces"":[""application/json"",""text/json"",""application/xml"",""text/xml""],""parameters"":[{""name"":""id"",""in"":""path"",""required"":true,""type"":""string""}],""responses"":{""200"":{""description"":""OK"",""schema"":{""$ref"":""#/definitions/Node""}},""404"":{""description"":""Specified node does not exist""},""500"":{""description"":""Internal Server Error"",""schema"":{""$ref"":""#/definitions/Error""}},""401"":{""description"":""EULA has not been accepted""},""402"":{""description"":""Invalid or expired license""}},""deprecated"":false}},""/p/pits"":{""get"":{""tags"":[""Pits""],""summary"":""Gets the list of all pits."",""description"":""The result does not included configuration variables and monitoring configuration."",""operationId"":""Pits_Get"",""consumes"":[],""produces"":[""application/json"",""text/json"",""application/xml"",""text/xml""],""responses"":{""200"":{""description"":""OK"",""schema"":{""type"":""array"",""items"":{""$ref"":""#/definitions/LibraryPit""}}},""500"":{""description"":""Internal Server Error"",""schema"":{""$ref"":""#/definitions/Error""}},""401"":{""description"":""EULA has not been accepted""},""402"":{""description"":""Invalid or expired license""}},""deprecated"":false},""post"":{""tags"":[""Pits""],""summary"":""Create a new pit configuration from a library pit."",""operationId"":""Pits_Post"",""consumes"":[""application/json"",""text/json"",""application/xml"",""text/xml"",""application/x-www-form-urlencoded""],""produces"":[""application/json"",""text/json"",""application/xml"",""text/xml""],""parameters"":[{""name"":""data"",""in"":""body"",""description"":""Source pit and destination configuration information."",""required"":true,""schema"":{""$ref"":""#/definitions/PitCopy""}}],""responses"":{""200"":{""description"":""OK"",""schema"":{""$ref"":""#/definitions/Pit""}},""400"":{""description"":""Request did not contain a valid PitCopy""},""403"":{""description"":""Access denied when saving config""},""404"":{""description"":""Specified pit or library does not exist""},""500"":{""description"":""Internal Server Error"",""schema"":{""$ref"":""#/definitions/Error""}},""401"":{""description"":""EULA has not been accepted""},""402"":{""description"":""Invalid or expired license""}},""deprecated"":false}},""/p/pits/{id}"":{""get"":{""tags"":[""Pits""],""summary"":""Get the details for a specific pit configuration."",""description"":""The result does includes configuration variables and monitoring configuration."",""operationId"":""Pits_Get"",""consumes"":[],""produces"":[""application/json"",""text/json"",""application/xml"",""text/xml""],""parameters"":[{""name"":""id"",""in"":""path"",""description"":""Pit identifier."",""required"":true,""type"":""string""}],""responses"":{""200"":{""description"":""OK"",""schema"":{""$ref"":""#/definitions/Pit""}},""404"":{""description"":""Specified pit does not exist""},""500"":{""description"":""Internal Server Error"",""schema"":{""$ref"":""#/definitions/Error""}},""401"":{""description"":""EULA has not been accepted""},""402"":{""description"":""Invalid or expired license""}},""deprecated"":false},""post"":{""tags"":[""Pits""],""summary"":""Update the configuration for a specific pit"",""description"":""This is how you save new configuration variables and monitors."",""operationId"":""Pits_Post"",""consumes"":[""application/json"",""text/json"",""application/xml"",""text/xml"",""application/x-www-form-urlencoded""],""produces"":[""application/json"",""text/json"",""application/xml"",""text/xml""],""parameters"":[{""name"":""id"",""in"":""path"",""description"":""The pit to update."",""required"":true,""type"":""string""},{""name"":""config"",""in"":""body"",""description"":""The variables and monitors configuration."",""required"":true,""schema"":{""$ref"":""#/definitions/PitConfig""}}],""responses"":{""200"":{""description"":""OK"",""schema"":{""$ref"":""#/definitions/Pit""}},""400"":{""description"":""Request did not contain a valid PitConfig""},""403"":{""description"":""Access denied when saving config""},""404"":{""description"":""Specified pit does not exist""},""500"":{""description"":""Internal Server Error"",""schema"":{""$ref"":""#/definitions/Error""}},""401"":{""description"":""EULA has not been accepted""},""402"":{""description"":""Invalid or expired license""}},""deprecated"":false}}},""definitions"":{""Job"":{""required"":[""id"",""name"",""speed"",""jobUrl"",""commands"",""metrics"",""firstNodeUrl"",""faultsUrl"",""targetUrl"",""targetConfigUrl"",""nodesUrl"",""peachUrl"",""reportUrl"",""packageFileUrl"",""status"",""mode"",""result"",""notes"",""user"",""iterationCount"",""startDate"",""stopDate"",""runtime"",""faultCount"",""peachVersion"",""tags"",""groups"",""hasMetrics"",""pitUrl"",""seed"",""rangeStart"",""rangeStop"",""duration"",""dryRun""],""type"":""object"",""properties"":{""id"":{""type"":""string""},""name"":{""description"":""The human readable name for the job"",""type"":""string"",""readOnly"":true},""speed"":{""format"":""int64"",""description"":""The average speed of the job in iterations per hour"",""type"":""integer"",""readOnly"":true},""jobUrl"":{""description"":""The URL of this job"",""type"":""string""},""commands"":{""$ref"":""#/definitions/JobCommands"",""description"":""URLs used to control a running job.""},""metrics"":{""$ref"":""#/definitions/JobMetrics"",""description"":""URLs to associated metrics""},""firstNodeUrl"":{""description"":""The URL for getting test results"",""type"":""string""},""faultsUrl"":{""description"":""The URL of faults from job"",""type"":""string""},""targetUrl"":{""description"":""The URL of the target this job is fuzzing"",""type"":""string""},""targetConfigUrl"":{""description"":""The URL of the target configuration for this job"",""type"":""string""},""nodesUrl"":{""description"":""The URL that returns a list of nodes used by this job"",""type"":""string""},""peachUrl"":{""description"":""The URL of the specific version of peach for this job"",""type"":""string""},""reportUrl"":{""description"":""The URL of the version of final report for this job"",""type"":""string""},""packageFileUrl"":{""description"":""The URL of the version of the package containing all job inputs"",""type"":""string""},""status"":{""description"":""The status of this job record"",""enum"":[""stopped"",""starting"",""running"",""paused"",""stopping""],""type"":""string""},""mode"":{""description"":""The mode that this job is operating under"",""enum"":[""preparing"",""recording"",""fuzzing"",""searching"",""reproducing"",""reporting""],""type"":""string""},""result"":{""description"":""The result of the job.\r\n            Only set when the Status is Stopped.\r\n            Otherwise is null and omitted from the JSON."",""type"":""string""},""notes"":{""description"":""Fuzzing notes associated with the job"",""type"":""string""},""user"":{""description"":""User that started the fuzzing job"",""type"":""string""},""iterationCount"":{""format"":""int64"",""description"":""How many iterations of fuzzing have been completed"",""type"":""integer""},""startDate"":{""format"":""date-time"",""description"":""The date the job was started"",""type"":""string""},""stopDate"":{""format"":""date-time"",""description"":""The date the job ended"",""type"":""string""},""runtime"":{""format"":""int64"",""description"":""The number of seconds the job has been running for"",""type"":""integer""},""faultCount"":{""format"":""int64"",""description"":""How many faults have been detected"",""type"":""integer""},""peachVersion"":{""description"":""The version of peach that ran the job."",""type"":""string""},""tags"":{""description"":""List of tags associated with this job"",""type"":""array"",""items"":{""$ref"":""#/definitions/Tag""}},""groups"":{""description"":""ACL for this job"",""type"":""array"",""items"":{""$ref"":""#/definitions/Group""}},""hasMetrics"":{""description"":""Indicates if metrics are being collected for the job"",""type"":""boolean"",""readOnly"":true},""pitUrl"":{""description"":""The URL of the specific version of the pit for this job"",""type"":""string""},""seed"":{""format"":""int64"",""description"":""The random seed being used by the fuzzing job"",""type"":""integer""},""rangeStart"":{""format"":""int64"",""description"":""Optional starting iteration number"",""type"":""integer""},""rangeStop"":{""format"":""int64"",""description"":""Optional ending iteration number"",""type"":""integer""},""duration"":{""description"":""Optional duration for how long to run the fuzzer."",""type"":""string""},""dryRun"":{""description"":""Determines whether the job is a test run or an actual fuzzing session."",""type"":""boolean""}}},""JobCommands"":{""required"":[""stopUrl"",""continueUrl"",""pauseUrl"",""killUrl""],""type"":""object"",""properties"":{""stopUrl"":{""description"":""The URL used to stop this job."",""type"":""string""},""continueUrl"":{""description"":""The URL used to continue this job."",""type"":""string""},""pauseUrl"":{""description"":""The URL used to pause this job."",""type"":""string""},""killUrl"":{""description"":""The URL used to kill this job."",""type"":""string""}}},""JobMetrics"":{""required"":[""bucketTimeline"",""faultTimeline"",""mutators"",""elements"",""dataset"",""states"",""buckets"",""iterations"",""fields""],""type"":""object"",""properties"":{""bucketTimeline"":{""description"":""The URL of bucket timeline metrics."",""type"":""string""},""faultTimeline"":{""description"":""The URL of fault timeline metrics."",""type"":""string""},""mutators"":{""description"":""The URL of mutator metrics."",""type"":""string""},""elements"":{""description"":""The URL of fuzzed elements metrics."",""type"":""string""},""dataset"":{""description"":""The URL of selected data sets metrics."",""type"":""string""},""states"":{""description"":""The URL of state execution metrics."",""type"":""string""},""buckets"":{""description"":""The URL of fault bucket metrics."",""type"":""string""},""iterations"":{""description"":""The URL of iteration metrics."",""type"":""string""},""fields"":{""description"":""The URL of field metrics."",""type"":""string""}}},""Tag"":{""description"":""An arbitrary tag that is included with various models."",""required"":[""name"",""values""],""type"":""object"",""properties"":{""name"":{""description"":""The name of this tag."",""type"":""string""},""values"":{""description"":""The values of this tag."",""type"":""array"",""items"":{""type"":""string""}}}},""Group"":{""required"":[""groupUrl"",""access""],""type"":""object"",""properties"":{""groupUrl"":{""type"":""string""},""access"":{""enum"":[""none"",""read"",""write""],""type"":""string""}}},""Error"":{""default"":{""ErrorMessage"":""Error message goes here."",""FullException"":""Full exception output goes here.""},""required"":[""errorMessage"",""fullException""],""type"":""object"",""properties"":{""errorMessage"":{""description"":""The error message."",""type"":""string""},""fullException"":{""description"":""Full stacktrace of the error."",""type"":""string""}}},""JobRequest"":{""required"":[""pitUrl"",""seed"",""rangeStart"",""rangeStop"",""duration"",""dryRun""],""type"":""object"",""properties"":{""pitUrl"":{""description"":""The URL of the specific version of the pit for this job"",""type"":""string""},""seed"":{""format"":""int64"",""description"":""The random seed being used by the fuzzing job"",""type"":""integer""},""rangeStart"":{""format"":""int64"",""description"":""Optional starting iteration number"",""type"":""integer""},""rangeStop"":{""format"":""int64"",""description"":""Optional ending iteration number"",""type"":""integer""},""duration"":{""description"":""Optional duration for how long to run the fuzzer."",""type"":""string""},""dryRun"":{""description"":""Determines whether the job is a test run or an actual fuzzing session."",""type"":""boolean""}}},""Object"":{""required"":[],""type"":""object"",""properties"":{}},""TestResult"":{""required"":[""status"",""events"",""log"",""logUrl""],""type"":""object"",""properties"":{""status"":{""description"":""The overall status of the test result"",""enum"":[""active"",""pass"",""fail""],""type"":""string""},""events"":{""description"":""The events that make up the test reslt"",""type"":""array"",""items"":{""$ref"":""#/definitions/TestEvent""}},""log"":{""description"":""The debug log from the test run"",""type"":""string""},""logUrl"":{""description"":""URL to the debug log from the test run"",""type"":""string""}}},""TestEvent"":{""required"":[""id"",""jobId"",""status"",""short"",""description"",""resolve""],""type"":""object"",""properties"":{""id"":{""format"":""int64"",""description"":""Identifier of event"",""type"":""integer""},""jobId"":{""type"":""string""},""status"":{""description"":""Status of event"",""enum"":[""active"",""pass"",""fail""],""type"":""string""},""short"":{""description"":""Short description of event"",""type"":""string""},""description"":{""description"":""Long description of event"",""type"":""string""},""resolve"":{""description"":""How to resolve the event if it is an issue"",""type"":""string""}}},""FaultSummary"":{""required"":[""id"",""faultUrl"",""archiveUrl"",""reproducible"",""iteration"",""flags"",""timeStamp"",""source"",""exploitability"",""majorHash"",""minorHash""],""type"":""object"",""properties"":{""id"":{""format"":""int64"",""description"":""Unique ID for this fault."",""type"":""integer""},""faultUrl"":{""description"":""The URL to the FaultDetail for this fault."",""type"":""string""},""archiveUrl"":{""description"":""The URL to download a zip archive of the entire fault data"",""type"":""string""},""reproducible"":{""description"":""Was this fault reproducible."",""type"":""boolean""},""iteration"":{""format"":""int64"",""description"":""The iteration this fault was detected on."",""type"":""integer""},""flags"":{""description"":""The type of iteration this fault was detected on."",""enum"":[""none"",""control"",""record""],""type"":""string""},""timeStamp"":{""format"":""date-time"",""description"":""The time this fault was recorded at."",""type"":""string""},""source"":{""description"":""The monitor that generated this fault."",""type"":""string""},""exploitability"":{""description"":""An exploitablilty rating of this fault."",""type"":""string""},""majorHash"":{""description"":""The major hash for this fault."",""type"":""string""},""minorHash"":{""description"":""The minor hash for this fault."",""type"":""string""}}},""Fault"":{""required"":[""mustStop"",""iteration"",""iterationStart"",""iterationStop"",""controlIteration"",""controlRecordingIteration"",""type"",""detectionSource"",""monitorName"",""agentName"",""title"",""description"",""majorHash"",""minorHash"",""exploitability"",""folderName"",""collectedData"",""states""],""type"":""object"",""properties"":{""mustStop"":{""type"":""boolean""},""iteration"":{""format"":""int32"",""type"":""integer""},""iterationStart"":{""format"":""int32"",""type"":""integer""},""iterationStop"":{""format"":""int32"",""type"":""integer""},""controlIteration"":{""type"":""boolean""},""controlRecordingIteration"":{""type"":""boolean""},""type"":{""enum"":[""unknown"",""fault"",""data""],""type"":""string""},""detectionSource"":{""type"":""string""},""monitorName"":{""type"":""string""},""agentName"":{""type"":""string""},""title"":{""type"":""string""},""description"":{""type"":""string""},""majorHash"":{""type"":""string""},""minorHash"":{""type"":""string""},""exploitability"":{""type"":""string""},""folderName"":{""type"":""string""},""collectedData"":{""type"":""array"",""items"":{""$ref"":""#/definitions/Data""}},""states"":{""type"":""array"",""items"":{""$ref"":""#/definitions/State""}}}},""Data"":{""required"":[""key"",""value"",""path""],""type"":""object"",""properties"":{""key"":{""type"":""string""},""value"":{""type"":""string""},""path"":{""type"":""string""}}},""State"":{""required"":[""name"",""actions""],""type"":""object"",""properties"":{""name"":{""type"":""string""},""actions"":{""type"":""array"",""items"":{""$ref"":""#/definitions/Action""}}}},""Action"":{""required"":[""name"",""type"",""models""],""type"":""object"",""properties"":{""name"":{""type"":""string""},""type"":{""type"":""string""},""models"":{""type"":""array"",""items"":{""$ref"":""#/definitions/Model""}}}},""Model"":{""required"":[""dataSet"",""parameter"",""name"",""mutations""],""type"":""object"",""properties"":{""dataSet"":{""type"":""string""},""parameter"":{""type"":""string""},""name"":{""type"":""string""},""mutations"":{""type"":""array"",""items"":{""$ref"":""#/definitions/Mutation""}}}},""Mutation"":{""required"":[""element"",""mutator""],""type"":""object"",""properties"":{""element"":{""type"":""string""},""mutator"":{""type"":""string""}}},""FaultTimelineMetric"":{""required"":[""date"",""faultCount""],""type"":""object"",""properties"":{""date"":{""format"":""date-time"",""type"":""string""},""faultCount"":{""format"":""int64"",""type"":""integer""}}},""BucketTimelineMetric"":{""required"":[""label"",""iteration"",""time"",""faultCount""],""type"":""object"",""properties"":{""label"":{""type"":""string""},""iteration"":{""format"":""int64"",""type"":""integer""},""time"":{""format"":""date-time"",""type"":""string""},""faultCount"":{""format"":""int64"",""type"":""integer""}}},""MutatorMetric"":{""required"":[""mutator"",""elementCount"",""iterationCount"",""bucketCount"",""faultCount""],""type"":""object"",""properties"":{""mutator"":{""type"":""string""},""elementCount"":{""format"":""int64"",""type"":""integer""},""iterationCount"":{""format"":""int64"",""type"":""integer""},""bucketCount"":{""format"":""int64"",""type"":""integer""},""faultCount"":{""format"":""int64"",""type"":""integer""}}},""ElementMetric"":{""required"":[""state"",""action"",""element"",""iterationCount"",""bucketCount"",""faultCount""],""type"":""object"",""properties"":{""state"":{""type"":""string""},""action"":{""type"":""string""},""element"":{""type"":""string""},""iterationCount"":{""format"":""int64"",""type"":""integer""},""bucketCount"":{""format"":""int64"",""type"":""integer""},""faultCount"":{""format"":""int64"",""type"":""integer""}}},""StateMetric"":{""required"":[""state"",""executionCount""],""type"":""object"",""properties"":{""state"":{""type"":""string""},""executionCount"":{""format"":""int64"",""type"":""integer""}}},""DatasetMetric"":{""required"":[""dataset"",""iterationCount"",""bucketCount"",""faultCount""],""type"":""object"",""properties"":{""dataset"":{""type"":""string""},""iterationCount"":{""format"":""int64"",""type"":""integer""},""bucketCount"":{""format"":""int64"",""type"":""integer""},""faultCount"":{""format"":""int64"",""type"":""integer""}}},""BucketMetric"":{""required"":[""bucket"",""mutator"",""element"",""iterationCount"",""faultCount""],""type"":""object"",""properties"":{""bucket"":{""type"":""string""},""mutator"":{""type"":""string""},""element"":{""type"":""string""},""iterationCount"":{""format"":""int64"",""type"":""integer""},""faultCount"":{""format"":""int64"",""type"":""integer""}}},""IterationMetric"":{""required"":[""state"",""action"",""parameter"",""element"",""mutator"",""dataset"",""iterationCount""],""type"":""object"",""properties"":{""state"":{""type"":""string""},""action"":{""type"":""string""},""parameter"":{""type"":""string""},""element"":{""type"":""string""},""mutator"":{""type"":""string""},""dataset"":{""type"":""string""},""iterationCount"":{""format"":""int64"",""type"":""integer""}}},""Library"":{""required"":[""libraryUrl"",""name"",""description"",""locked"",""versions"",""groups"",""user"",""timestamp""],""type"":""object"",""properties"":{""libraryUrl"":{""type"":""string""},""name"":{""type"":""string""},""description"":{""type"":""string""},""locked"":{""type"":""boolean""},""versions"":{""type"":""array"",""items"":{""$ref"":""#/definitions/LibraryVersion""}},""groups"":{""type"":""array"",""items"":{""$ref"":""#/definitions/Group""}},""user"":{""type"":""string""},""timestamp"":{""format"":""date-time"",""type"":""string""}}},""LibraryVersion"":{""required"":[""version"",""locked"",""pits""],""type"":""object"",""properties"":{""version"":{""format"":""int32"",""type"":""integer""},""locked"":{""type"":""boolean""},""pits"":{""type"":""array"",""items"":{""$ref"":""#/definitions/LibraryPit""}}}},""LibraryPit"":{""required"":[""id"",""pitUrl"",""name"",""description"",""tags"",""locked""],""type"":""object"",""properties"":{""id"":{""type"":""string""},""pitUrl"":{""type"":""string""},""name"":{""type"":""string""},""description"":{""type"":""string""},""tags"":{""type"":""array"",""items"":{""$ref"":""#/definitions/Tag""}},""locked"":{""type"":""boolean""}}},""License"":{""required"":[""isValid"",""isInvalid"",""isMissing"",""isExpired"",""errorText"",""expiration"",""version""],""type"":""object"",""properties"":{""isValid"":{""description"":""Is the license falid."",""type"":""boolean""},""isInvalid"":{""description"":""Is the license invalid."",""type"":""boolean""},""isMissing"":{""description"":""Is the license missing."",""type"":""boolean""},""isExpired"":{""description"":""Is the license expired."",""type"":""boolean""},""errorText"":{""description"":""Human readable error for why license is not valid."",""type"":""string""},""expiration"":{""format"":""date-time"",""description"":""When the license expires."",""type"":""string""},""version"":{""description"":""The features associated with this license."",""enum"":[""enterprise"",""distributed"",""professionalWithConsulting"",""professional"",""trialAllPits"",""trial"",""academic"",""testSuites"",""studio"",""developer"",""unknown""],""type"":""string""}}},""Node"":{""required"":[""nodeUrl"",""name"",""mac"",""ip"",""tags"",""status"",""version"",""timestamp"",""jobUrl""],""type"":""object"",""properties"":{""nodeUrl"":{""description"":""The URL of this node"",""type"":""string""},""name"":{""type"":""string""},""mac"":{""type"":""string""},""ip"":{""type"":""string""},""tags"":{""type"":""array"",""items"":{""$ref"":""#/definitions/Tag""}},""status"":{""enum"":[""alive"",""late"",""running""],""type"":""string""},""version"":{""type"":""string""},""timestamp"":{""format"":""date-time"",""type"":""string""},""jobUrl"":{""type"":""string""}}},""PitCopy"":{""required"":[""legacyPitUrl"",""pitUrl"",""name"",""description""],""type"":""object"",""properties"":{""legacyPitUrl"":{""type"":""string""},""pitUrl"":{""type"":""string""},""name"":{""type"":""string""},""description"":{""type"":""string""}}},""Pit"":{""required"":[""peaches"",""user"",""timestamp"",""config"",""agents"",""weights"",""metadata"",""id"",""pitUrl"",""name"",""description"",""tags"",""locked""],""type"":""object"",""properties"":{""peaches"":{""type"":""array"",""items"":{""$ref"":""#/definitions/PeachVersion""}},""user"":{""type"":""string""},""timestamp"":{""format"":""date-time"",""type"":""string""},""config"":{""type"":""array"",""items"":{""$ref"":""#/definitions/Param""}},""agents"":{""type"":""array"",""items"":{""$ref"":""#/definitions/Agent""}},""weights"":{""type"":""array"",""items"":{""$ref"":""#/definitions/PitWeight""}},""metadata"":{""$ref"":""#/definitions/PitMetadata""},""id"":{""type"":""string""},""pitUrl"":{""type"":""string""},""name"":{""type"":""string""},""description"":{""type"":""string""},""tags"":{""type"":""array"",""items"":{""$ref"":""#/definitions/Tag""}},""locked"":{""type"":""boolean""}}},""PeachVersion"":{""required"":[""peachUrl"",""major"",""minor"",""build"",""revision""],""type"":""object"",""properties"":{""peachUrl"":{""type"":""string""},""major"":{""type"":""string""},""minor"":{""type"":""string""},""build"":{""type"":""string""},""revision"":{""type"":""string""}}},""Param"":{""description"":""The most basic key/value pair used for all parameters."",""required"":[""key"",""name"",""description"",""value""],""type"":""object"",""properties"":{""key"":{""description"":""Machine name used by peach.\r\n            This wil not include spaces."",""type"":""string""},""name"":{""description"":""The human name of this parameter."",""type"":""string""},""description"":{""description"":""Description of the parameter"",""type"":""string""},""value"":{""description"":""Parameter value."",""type"":""string""}}},""Agent"":{""description"":""Represents an agent in a pit file.\r\n            Contains agent the location and list of monitors."",""required"":[""name"",""agentUrl"",""monitors""],""type"":""object"",""properties"":{""name"":{""description"":""Name of the agent for reference in tests"",""type"":""string""},""agentUrl"":{""description"":""The agent location including the agent channel"",""type"":""string""},""monitors"":{""description"":""The list of monitors associated with the agent"",""type"":""array"",""items"":{""$ref"":""#/definitions/Monitor""}}}},""PitWeight"":{""required"":[""id"",""weight""],""type"":""object"",""properties"":{""id"":{""type"":""string""},""weight"":{""format"":""int32"",""type"":""integer""}}},""PitMetadata"":{""required"":[""defines"",""calls"",""monitors"",""fields""],""type"":""object"",""properties"":{""defines"":{""type"":""array"",""items"":{""$ref"":""#/definitions/ParamDetail""}},""calls"":{""type"":""array"",""items"":{""type"":""string""}},""monitors"":{""type"":""array"",""items"":{""$ref"":""#/definitions/ParamDetail""}},""fields"":{""type"":""array"",""items"":{""$ref"":""#/definitions/PitField""}}}},""Monitor"":{""description"":""Represents a single monitor instance."",""required"":[""monitorClass"",""name"",""map""],""type"":""object"",""properties"":{""monitorClass"":{""description"":""The class of the monitor"",""type"":""string""},""name"":{""description"":""User friendly name of the monitor instance"",""type"":""string""},""map"":{""type"":""array"",""items"":{""$ref"":""#/definitions/Param""}}}},""ParamDetail"":{""description"":""A configuration parameter passed to a monitor.\r\n            Parameter is a more verbose param."",""required"":[""type"",""items"",""options"",""defaultValue"",""min"",""max"",""optional"",""collapsed"",""os"",""key"",""name"",""description"",""value""],""type"":""object"",""properties"":{""type"":{""description"":""The type of the parameter"",""enum"":[""string"",""hex"",""range"",""ipv4"",""ipv6"",""hwaddr"",""iface"",""enum"",""bool"",""user"",""system"",""call"",""group"",""space"",""monitor""],""type"":""string""},""items"":{""description"":"""",""type"":""array"",""items"":{""$ref"":""#/definitions/ParamDetail""}},""options"":{""description"":""List of values for enum types"",""type"":""array"",""items"":{""type"":""string""}},""defaultValue"":{""description"":""Is this parameter required?"",""type"":""string""},""min"":{""format"":""int64"",""description"":"""",""type"":""integer""},""max"":{""format"":""int64"",""description"":"""",""type"":""integer""},""optional"":{""description"":"""",""type"":""boolean""},""collapsed"":{""description"":"""",""type"":""boolean""},""os"":{""description"":""The set of operating systems that this monitor supports."",""type"":""string""},""key"":{""description"":""Machine name used by peach.\r\n            This wil not include spaces."",""type"":""string""},""name"":{""description"":""The human name of this parameter."",""type"":""string""},""description"":{""description"":""Description of the parameter"",""type"":""string""},""value"":{""description"":""Parameter value."",""type"":""string""}}},""PitField"":{""required"":[""id"",""fields""],""type"":""object"",""properties"":{""id"":{""type"":""string""},""fields"":{""type"":""array"",""items"":{""$ref"":""#/definitions/PitField""}}}},""PitConfig"":{""required"":[""id"",""name"",""description"",""originalPit"",""config"",""agents"",""weights""],""type"":""object"",""properties"":{""id"":{""type"":""string""},""name"":{""type"":""string""},""description"":{""type"":""string""},""originalPit"":{""type"":""string""},""config"":{""type"":""array"",""items"":{""$ref"":""#/definitions/Param""}},""agents"":{""type"":""array"",""items"":{""$ref"":""#/definitions/Agent""}},""weights"":{""type"":""array"",""items"":{""$ref"":""#/definitions/PitWeight""}}}}}}";
	}
}
