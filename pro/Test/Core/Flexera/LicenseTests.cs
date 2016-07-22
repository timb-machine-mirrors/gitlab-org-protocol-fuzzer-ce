using System;
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
		const string ServerUrl = "https://flex1253-uat.compliance.flexnetoperations.com/instances/278QV9C68FEK/request";

		[Test]
		public void TestCapabilityRequest()
		{
			var feature = "Test-Metered";
			RunHost("TEST-1", "3489-9efc-c700-48e0-b96e-1518-7b43-9644", feature);
			RunHost("TEST-2", "821a-de30-c29c-4ae9-a195-3f76-4d9c-cc6a", feature);
		}

		private static void RunHost(string hostId, string rightsId, string feature)
		{
			using (var licensing = LicensingFactory.GetLicensing(IdentityClient_UAT.IdentityData, null, hostId))
			{
				Console.WriteLine("RunHost> request {0}, {1}", hostId, feature);
				DoRequest(licensing, rightsId, feature, 10);
				Console.WriteLine("----------");

				var acquired = 0;
				try
				{
					while (true)
					{
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
				Assert.AreEqual(10, acquired);

				//Console.WriteLine("RunHost> return {0}, {1}", hostId, feature);
				//DoRequest(licensing, rightsId, feature, -2);
				//Console.WriteLine("----------");
			}
			Console.WriteLine("##########");
		}

		private static ICapabilityResponse DoRequest(ILicensing licensing, string rightsId, string feature, int count)
		{
			var options = licensing.LicenseManager.CreateCapabilityRequestOptions();
			options.Operation = CapabilityRequestOperation.Request;
			options.AddRightsId(rightsId, 1);
			options.RequestorId = "LicenseTests";
			var data = new FeatureData(feature, "1.0", count);
			options.AddDesiredFeature(data);
			options.ForceResponse = true;

			var request = licensing.LicenseManager.CreateCapabilityRequest(options);
			var binRequest = request.ToArray();

			//Console.WriteLine("Sending request: {0} bytes", binRequest.Length);

			byte[] binResponse;
			CommFactory.Create(ServerUrl)
				.SendBinaryMessage(binRequest, out binResponse);

			var response = licensing.LicenseManager.ProcessCapabilityResponse(binResponse);
			DumpResponse(response, licensing);
			return response;
		}

		private static void DumpResponse(ICapabilityResponse response, ILicensing licensing)
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