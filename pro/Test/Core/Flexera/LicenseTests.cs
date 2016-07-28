using System;
using FlexNet.Operations;
using FlxDotNetClient;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Test;
using Peach.Pro.Core.License;

namespace Peach.Pro.Test.Core.Flexera
{
	[TestFixture]
	[Quick]
	internal class LicenseTests
	{
		[Test]
		public void TestMeteredUsage()
		{
			RunHost(
				"https://flex1253.compliance.flexnetoperations.com/instances/LBSECNM5BMBG/request",
				"TEST-1", 
				"b7d7-7241-e5f6-41fc-9a85-96bf-75e8-a1c6", 
				"Test-Metered", 
				10, 
				10
			);
		}

		[Test]
		public void TestPrepaidUsage()
		{
			ReplenishLineItem(
				"5e7b-a5d7-b366-443f-b087-1926-3422-7fa1",
				"95b4-af92-9c68-4973-aebc-43cc-d1cf-6685",
				10
			);

			RunHost(
				"https://flex1253.compliance.flexnetoperations.com/instances/WV26ZW5K4GRP/request",
				"TEST-2",
				"8171-8bf5-9803-470b-82ad-9f49-c56b-1cba", 
				"Test-Metered", 
				15, 
				10
			);
		}

		[Test]
		public void TestLocalLicenseServer_Metered()
		{
			RunHost(
				"http://10.0.1.96:7070/request",
				"TEST-3",
				"98c6-3b2c-a5de-4eab-8ad2-7a1c-cc2d-9369",
				"Test-Metered",
				10,
				10
			);
		}

		private void ReplenishLineItem(string entitlementId, string activationId, int increment)
		{
			using (var service = Factory.Create<EntitlementOrderService>(EnvironmentType.Production))
			{
				var query = service.getEntitlementLineItemPropertiesQuery(new searchEntitlementLineItemPropertiesRequestType
				{
					pageNumber = "1",
					batchSize = "1",
					queryParams = new searchActivatableItemDataType
					{
						entitlementId = new SimpleQueryType
						{
							searchType = simpleSearchType.EQUALS,
							value = entitlementId
						},
						activationId = new SimpleQueryType
						{
							searchType = simpleSearchType.EQUALS,
							value = activationId
						},
					},
					entitlementLineItemResponseConfig = new entitlementLineItemResponseConfigRequestType
					{
						entitlementId = true,
						entitlementIdSpecified = true,
						activationId = true,
						activationIdSpecified = true,
						numberOfCopies = true,
						numberOfCopiesSpecified = true,
					}
				});

				Assert.AreEqual(StatusType.SUCCESS, query.statusInfo.status, query.statusInfo.reason);

				var lineItem = query.entitlementLineItem[0];
				var count = Convert.ToInt32(lineItem.numberOfCopies) + increment;

				var update = service.updateEntitlementLineItem(new[]
				{
					new updateEntitlementLineItemDataType
					{
						entitlementIdentifier = new entitlementIdentifierType
						{
							uniqueId = lineItem.entitlementId.uniqueId
						},
						autoDeploy = true,
						autoDeploySpecified = true,
						lineItemData = new[]
						{
							new updateLineItemDataType
							{
								lineItemIdentifier = new entitlementLineItemIdentifierType
								{
									uniqueId = lineItem.activationId.uniqueId
								},
								numberOfCopies = count.ToString(),
							}
						}
					}
				});

				Assert.AreEqual(StatusType.SUCCESS, update.statusInfo.status, update.statusInfo.reason);
			}
		}

		private static void RunHost(
			string serverUrl, 
			string hostId,
			string rightsId, 
			string feature, 
			int count, 
			int expected)
		{
			using (var licensing = LicensingFactory.GetLicensing(IdentityClient_Production.IdentityData, null, hostId))
			{
				Console.WriteLine("RunHost> request {0}, {1}", hostId, feature);
				DoRequest(licensing, serverUrl, rightsId, feature, count);
				Console.WriteLine("----------");

				DumpTrustedStore(licensing);

				var acquired = 0;
				try
				{
					while (true)
					{
						Console.WriteLine("Acquire: {0}", feature);
						licensing.LicenseManager.Acquire(feature);
						acquired++;
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine("Exception: {0}", ex);
					Console.WriteLine("Acquired: {0}", acquired);
				}
				finally
				{
					licensing.LicenseManager.ReturnAllLicenses();
				}
				Assert.AreEqual(expected, acquired);

				DumpTrustedStore(licensing);
			}
			Console.WriteLine("##########");
		}

		private static ICapabilityResponse DoRequest(
			ILicensing licensing, 
			string serverUrl, 
			string rightsId, 
			string feature, 
			int count)
		{
			var options = licensing.LicenseManager.CreateCapabilityRequestOptions();
			options.Operation = CapabilityRequestOperation.Request;
			options.AddRightsId(rightsId, 1);
			options.RequestorId = "LicenseTests";
			var featureOptions = licensing.LicenseManager.CreateDesiredFeatureOptions();
			featureOptions.PartialFulfillment = true;
			var data = new FeatureData(feature, "1.0", count, featureOptions);
			options.AddDesiredFeature(data);
			options.ForceResponse = true;

			var request = licensing.LicenseManager.CreateCapabilityRequest(options);
			var binRequest = request.ToArray();

			//Console.WriteLine("Sending request: {0} bytes", binRequest.Length);

			byte[] binResponse;
			CommFactory.Create(serverUrl)
				.SendBinaryMessage(binRequest, out binResponse);

			var response = licensing.LicenseManager.ProcessCapabilityResponse(binResponse);
			DumpResponse(response);
			return response;
		}

		private static void DumpResponse(ICapabilityResponse response)
		{
			Console.WriteLine("---Response---: {0}", response == null ? "<null>" : response.ToString());
			if (response != null)
			{
				foreach (var item in response.Status)
				{
					Console.WriteLine("Status Item> category: {0}, code: {1}{2}, details: {3}",
						item.TypeDescription,
						(int)item.Code,
						string.IsNullOrEmpty(item.CodeDescription) ? "" : " ({0})".Fmt(item.CodeDescription),
						item.Details
						);
				}

				Console.WriteLine("Features");
				foreach (var feature in response.FeatureCollection)
				{
					Console.WriteLine("    {0} v{1}: {2}, available: {3}",
						feature.Name,
						feature.Version,
						feature.Count,
						feature.AvailableAcquisitionCount
					);
				}
			}
		}

		private static void DumpTrustedStore(ILicensing licensing)
		{
			Console.WriteLine("---LicenseManager---");

			Console.WriteLine("Features");
			foreach (var features in licensing.LicenseManager.GetFeatureCollection())
			{
				Console.WriteLine("  {0}", features.Key);
				foreach (var feature in features.Value)
				{
					Console.WriteLine("    {0} v{1}: {2}, available: {3}",
						feature.Name,
						feature.Version,
						feature.Count,
						feature.AvailableAcquisitionCount
					);
				}
			}
		}
	}
}