using System;
using System.Linq;
using System.Threading;
using FlexNet.Operations;
using FlxDotNetClient;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Test;
using Peach.Pro.Core.License;
using Usage = FlexNet.Operations.Usage;

namespace Peach.Pro.Test.Core.Flexera
{
	[TestFixture]
	[Quick]
	internal class LicenseTests
	{
		class TestConfig : ILicenseConfig
		{
			public string ActivationId { get; set; }
			public byte[] IdentityData { get; set; }
			public string LicenseUrl { get; set; }
			public string LicensePath { get; set; }
		}

		TempDirectory _tmpDir;

		[SetUp]
		public void SetUp()
		{
			_tmpDir = new TempDirectory();
		}

		[TearDown]
		public void TearDown()
		{
			_tmpDir.Dispose();
		}

		[Test]
		public void TestMeteredUsage()
		{
			const string Pit = "PeachPit-Net-HTTP_Server";
			var cfg = new TestConfig
			{
				LicensePath = _tmpDir.Path,
				IdentityData = IdentityClient_Production.IdentityData,
				ActivationId = "6075-cf0e-3d4e-4849-a4e6-564d-5e62-ff6c",
				LicenseUrl = "https://flex1253.compliance.flexnetoperations.com/instances/LBSECNM5BMBG/request",
			};

			var before = GetUsage("Test-Metered", "Peach-TestCase");

			using (var license = new FlexeraLicense(cfg))
			{
				license.Activate();

				using (var lease = license.CanUsePit(Pit))
					Assert.IsTrue(lease.IsValid);

				using (var lease = license.CanExportPit())
					Assert.IsFalse(lease.IsValid);

				using (var job = (FlexeraLicense.JobLicense)license.NewJob(Pit, "TestMeteredUsage", _tmpDir.Path))
				{
					for (var i = 0; i < 10; i++)
						Assert.IsTrue(job.CanExecuteTestCase());

					// force a license check
					job.LastCheckTime = DateTime.MinValue;

					for (var i = 0; i < 10; i++)
						Assert.IsTrue(job.CanExecuteTestCase());

					using (var lease = license.CanUsePit(Pit))
						Assert.IsTrue(lease.IsValid);

					using (var lease = license.CanExportPit())
						Assert.IsFalse(lease.IsValid);
				}
			}

			// We need to wait for the Flexera backend to process the usage reports
			// 10 seconds seems to fail sometimes.
			Thread.Sleep(20000);

			var after = GetUsage("Test-Metered", "Peach-TestCase");
			var delta = Convert.ToInt32(after.usageSinceReset - before.usageSinceReset);
			Assert.AreEqual(20, delta, "before: {0}, after: {1}".Fmt(before.usageSinceReset, after.usageSinceReset));
		}

		[Test]
		public void TestPrepaidUsage()
		{
			const string Pit = "PeachPit-Net-HTTP_Server";
			var cfg = new TestConfig
			{
				LicensePath = _tmpDir.Path,
				IdentityData = IdentityClient_Production.IdentityData,
				ActivationId = "7b7f-90aa-035f-4f39-bceb-1578-7a71-70ed",
				LicenseUrl = "https://flex1253.compliance.flexnetoperations.com/instances/WV26ZW5K4GRP/request",
			};

			ResetUsage(cfg.ActivationId, cfg.LicenseUrl);

			ReplenishLineItem(
				"5e7b-a5d7-b366-443f-b087-1926-3422-7fa1",
				"6171-07f4-6b39-45fd-bf30-f20e-c039-62e0",
				20
			);

			Console.WriteLine("Waiting...");
			Thread.Sleep(10000);
			Console.WriteLine();

			using (var license = new FlexeraLicense(cfg))
			{
				license.Activate();

				using (var lease = license.CanUsePit(Pit))
					Assert.IsTrue(lease.IsValid);

				using (var lease = license.CanExportPit())
					Assert.IsTrue(lease.IsValid);

				using (var job = (FlexeraLicense.JobLicense)license.NewJob(Pit, "TestPrepaidUsage", _tmpDir.Path))
				{
					for (var i = 0; i < 5; i++)
						Assert.IsTrue(job.CanExecuteTestCase());

					// force a license check
					job.LastCheckTime = DateTime.MinValue;

					for (var i = 0; i < 20; i++)
						Assert.IsTrue(job.CanExecuteTestCase());

					// force a license check
					job.LastCheckTime = DateTime.MinValue;

					Assert.IsFalse(job.CanExecuteTestCase());

					using (var lease = license.CanUsePit(Pit))
						Assert.IsTrue(lease.IsValid);

					using (var lease = license.CanExportPit())
						Assert.IsTrue(lease.IsValid);
				}
			}
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

		void ResetUsage(string rightsId, string serverUrl)
		{
			var usage = GetUsage("Test-Prepaid", "Peach-TestCase");
			var entitled = Convert.ToInt32(usage.entitled);
			var used = Convert.ToInt32(usage.usageSinceReset);
			var count = entitled - used;

			Console.WriteLine("ResetUsage: entitled: {0}, used: {1}, count: {2}", entitled, used, count);

			if (count <= 0)
				return;

			using (var licensing = LicensingFactory.GetLicensing(IdentityClient_Production.IdentityData, null, rightsId))
			{
				var options = licensing.LicenseManager.CreateCapabilityRequestOptions();
				options.Operation = CapabilityRequestOperation.Request;
				options.AddRightsId(rightsId, 1);
				var featureOptions = licensing.LicenseManager.CreateDesiredFeatureOptions();
				featureOptions.PartialFulfillment = true;
				var data = new FeatureData("Peach-TestCase", "1", count, featureOptions);
				options.AddDesiredFeature(data);
				options.ForceResponse = true;

				var request = licensing.LicenseManager.CreateCapabilityRequest(options);
				var binRequest = request.ToArray();

				byte[] binResponse;
				CommFactory.Create(serverUrl)
					.SendBinaryMessage(binRequest, out binResponse);

				var response = licensing.LicenseManager.ProcessCapabilityResponse(binResponse);
				DumpResponse(response);
			}
		}

		Usage GetUsage(string orgName, string meter)
		{
			using (var service = Factory.Create<UsageService>(EnvironmentType.Production))
			{
				var usage = service.getUsage(new GetUsageRequest
				{
					orgName = orgName
				});

				Assert.AreEqual(StatusType.SUCCESS, usage.statusInfoType.status, usage.statusInfoType.reason);

				return usage.usages.Single(x => x.meter == meter);
			}
		}

		private void ReplenishLineItem(string entitlementId, string activationId, int more)
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
				var count = Convert.ToInt32(lineItem.numberOfCopies) + more;

				Console.WriteLine("Replenishing: {0}", count);

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
			// 1. Activation
			// 2. IterationStart
			//    1. Check available count
			//    2. Request more if below low watermark
			//    3. Acquire
			// 3. SessionFinished
			//    1. ReturnAllLicenses
			//    2. Report

			using (var licensing = LicensingFactory.GetLicensing(IdentityClient_Production.IdentityData, null, hostId))
			{
				Console.WriteLine("RunHost> request {0}, {1}", hostId, feature);
				DoRequest(licensing, serverUrl, rightsId, feature, count);
				Console.WriteLine("----------");

				DumpTrustedStore(licensing);

				var acquired = 0;
				try
				{
					for (var i = 0; i < count; i++)
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
			options.AcquisitionId = "pit:config"; // Pit:Config
			options.RequestorId = "LicenseTests"; // JobID
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