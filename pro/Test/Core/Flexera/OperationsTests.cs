﻿using System.Linq;
using FlexNet.Operations;
using NUnit.Framework;
using Peach.Core.Test;

namespace Peach.Pro.Test.Core.Flexera
{
	[TestFixture]
	[Quick]
	public class OperationsTests
	{
		[Test]
		public void BasicTest()
		{
			var service = Factory.Create<ProductPackagingService>(EnvironmentType.UAT);

			var query = service.getFeaturesQuery(new getFeaturesQueryRequestType
			{
				pageNumber = "1",
				batchSize = "1",
				queryParams = new featureQueryParametersType
				{
					featureName = new SimpleQueryType
					{
						searchType = simpleSearchType.EQUALS,
						value = "test_feature",
					},
					version = new SimpleQueryType
					{
						searchType = simpleSearchType.EQUALS,
						value = "1.0",
					}
				}
			});
			Assert.AreEqual(StatusType.SUCCESS, query.statusInfo.status, query.statusInfo.reason);

			string featureId;
			if (!query.responseData.Any())
			{
				var feature = service.createFeature(new[]
				{
					new featureDataType
					{
						featureName = "test_feature",
						versionFormat = VersionFormatType.FIXED,
						version = "1.0",
					}
				});
				Assert.AreEqual(StatusType.SUCCESS, feature.statusInfo.status, feature.statusInfo.reason);
				featureId = feature.responseData[0].uniqueId;
			}
			else
			{
				featureId = query.responseData[0].uniqueId;
			}

			var model = service.getLicenseModelIdentifiers(new getModelIdentifiersRequestType
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
			Assert.AreEqual(StatusType.SUCCESS, model.statusInfo.status, model.statusInfo.reason);

			//var product = service.createProduct(new createProductDataType[] 
			//{
			//	new createProductDataType
			//	{
			//		productName = "test_product",
			//		version = "1.0",
			//		features = new featureIdentifierWithCountDataType[]
			//		{
			//			new featureIdentifierWithCountDataType
			//			{
			//				featureIdentifier = new featureIdentifierType
			//				{
			//					uniqueId = featureId,
			//				}
			//			}
			//		},
			//		licenseModels = new licenseModelIdentifierType[]
			//		{
			//			model.responseData[0].licenseModelIdentifier
			//		},
			//		//usedOnDevice = true,
			//	}
			//});
			//Assert.AreEqual(StatusType.SUCCESS, product.statusInfo.status, product.statusInfo.reason);

			var result = service.deleteFeature(new[]
			{
				new deleteFeatureDataType
				{
					featureIdentifier = new featureIdentifierType
					{
						uniqueId = featureId,
					}
				}
			});

			Assert.AreEqual(StatusType.SUCCESS, result.statusInfo.status, result.statusInfo.reason);
		}
	}
}