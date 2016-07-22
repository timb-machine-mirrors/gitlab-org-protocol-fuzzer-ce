using System;
using System.Linq;
//using System.Net;
using FlexNet.Operations;
using NUnit.Framework;
using Peach.Core.Test;

namespace Peach.Pro.Test.Core.Flexera
{
	[TestFixture]
	[Quick]
	public class OperationsTests
	{
		ProductPackagingService _productService;
		UserOrgHierarchyService _orgService;

		[SetUp]
		public void SetUp()
		{
			_productService = Factory.Create<ProductPackagingService>(EnvironmentType.UAT);
			//_productService.Proxy = new WebProxy("127.0.0.1", 8080);
			_orgService = Factory.Create<UserOrgHierarchyService>(EnvironmentType.UAT);
			//_orgService.Proxy = new WebProxy("127.0.0.1", 8080);
		}

		[TearDown]
		public void TearDown()
		{
			_productService.Dispose();
			_orgService.Dispose();
		}
		
		public string CreateFeature(string name, string version)
		{
			var query = _productService.getFeaturesQuery(new getFeaturesQueryRequestType
			{
				pageNumber = "1",
				batchSize = "1",
				queryParams = new featureQueryParametersType
				{
					featureName = new SimpleQueryType
					{
						searchType = simpleSearchType.EQUALS,
						value = name,
					},
					version = new SimpleQueryType
					{
						searchType = simpleSearchType.EQUALS,
						value = version,
					}
				}
			});

			if (query.statusInfo.status != StatusType.SUCCESS)
				throw new ApplicationException(query.statusInfo.reason);

			if (query.responseData.Any())
				return query.responseData[0].uniqueId;

			var feature = _productService.createFeature(new[]
			{
				new featureDataType
				{
					featureName = name,
					versionFormat = VersionFormatType.FIXED,
					version = version,
				}
			});

			if (feature.statusInfo.status != StatusType.SUCCESS)
				throw new ApplicationException(feature.statusInfo.reason);

			return feature.responseData[0].uniqueId;
		}

		public string CreateProduct(string name, string version, string modelName, string featureId)
		{
			var query = _productService.getProductsQuery(new getProductsQueryRequestType
			{
				pageNumber = "1",
				batchSize = "1",
				queryParams = new productQueryParametersType
				{
					productName = new SimpleQueryType
					{
						searchType = simpleSearchType.EQUALS,
						value = name
					},
					version = new SimpleQueryType
					{
						searchType = simpleSearchType.EQUALS,
						value = version
					}
				}
			});

			if (query.responseData.Any())
				return query.responseData[0].uniqueId;

			var model = _productService.getLicenseModelIdentifiers(new getModelIdentifiersRequestType
			{
				queryParams = new identifierQueryParametersType
				{
					name = new SimpleQueryType
					{
						searchType = simpleSearchType.EQUALS,
						value = modelName
					}
				}
			});
			if (model.statusInfo.status != StatusType.SUCCESS)
				throw new ApplicationException(model.statusInfo.reason);

			var product = _productService.createProduct(new createProductDataType[]
			{
				new createProductDataType
				{
					// required
					productName = name,
					version = version,
					// required for deployment
					features = new featureIdentifierWithCountDataType[]
					{
						new featureIdentifierWithCountDataType
						{
							featureIdentifier = new featureIdentifierType
							{
								uniqueId = featureId,
							},
							count = "1"
						}
					},
					licenseModels = new licenseModelIdentifierType[]
					{
						new licenseModelIdentifierType
						{
							uniqueId = model.responseData[0].licenseModelIdentifier.uniqueId
						}
					},
					usedOnDevice = true,
					usedOnDeviceSpecified = true,
					productCategory = "Test Product Line"
				}
			});

			if (product.statusInfo.status != StatusType.SUCCESS)
			{
				Console.WriteLine("Status: {0}, Reason: {1}", product.statusInfo.status, product.statusInfo.reason);
				foreach (var data in product.failedData)
				{
					Console.WriteLine("  {0}: {1}", data.product.productName, data.reason);
				}
			}

			if (product.statusInfo.status != StatusType.SUCCESS)
				throw new ApplicationException(product.statusInfo.reason);
			
			return product.responseData[0].uniqueId;
		}

		public void DeleteProduct(string productId)
		{
			var result = _productService.deleteProduct(new[]
			{
				new deleteProductDataType
				{
					productIdentifier = new productIdentifierType
					{
						uniqueId = productId
					}
				}
			});

			if (result.statusInfo.status != StatusType.SUCCESS)
				throw new ApplicationException(result.statusInfo.reason);
		}

		public void DeleteFeature(string featureId)
		{
			var result = _productService.deleteFeature(new[]
			{
				new deleteFeatureDataType
				{
					featureIdentifier = new featureIdentifierType
					{
						uniqueId = featureId,
					}
				}
			});

			if (result.statusInfo.status != StatusType.SUCCESS)
				throw new ApplicationException(result.statusInfo.reason);
		}

		public void CreateOrganization(string name)
		{
			_orgService.createOrganization(new[]
			{
				new organizationDataType
				{
					name = name,
					displayName = name,
					orgType = OrgType.CUSTOMER
				}
			});
		}

		public void CreateUser(string first, string last, string email, string orgId, string roleId)
		{
			_orgService.createUser(new[]
			{
				new createUserDataType
				{
					firstName = first,
					lastName = last,
					canLogin = true,
					emailAddress = email,
					orgRolesList = new[]
					{
						new createUserOrganizationType
						{
							organization = new organizationIdentifierType
							{
								uniqueId = orgId
							},
							roles = new[]
							{
								new roleIdentifierType
								{
									uniqueId = roleId
								}
							}
						}
					}
				}
			});
		}

		//[Test]
		//public void BasicTest()
		//{
		//	var baselineFeature = CreateFeature("Test-Baseline", "1.0");
		//	var baselineProduct = CreateProduct("Test-Baseline", "1.0", "Embedded Uncounted", baselineFeature);

		//	var meteredFeature = CreateFeature("Test-Metered", "1.0");
		//	var meteredProduct = CreateProduct("Test-Metered", "1.0", "Test Metered", meteredFeature);

		//	//DeleteProduct(meteredProduct);
		//	//DeleteFeature(meteredFeature);

		//	//DeleteProduct(baselineProduct);
		//	//DeleteFeature(baselineFeature);
		//}

		[Test]
		public void TestInvalidResponse()
		{
			// this test case is for Flexera support.

			_productService.getLicenseModelIdentifiers(new getModelIdentifiersRequestType
			{
				queryParams = new identifierQueryParametersType
				{
					name = new SimpleQueryType
					{
						searchType = simpleSearchType.EQUALS,
						value = "Embedded Uncounted"
					}
				}
			});

		}
	}
}
