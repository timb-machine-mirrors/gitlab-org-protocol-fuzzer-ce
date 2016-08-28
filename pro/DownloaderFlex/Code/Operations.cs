using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using FlexNet.Library;
using FlexNet.Operations;
using NLog;

namespace PeachDownloader
{
	public class Activation
	{
		public string OrgName;
		public string ActivationId;
		public string Product;
		public string LicenseServerUrl;
		public string[] Pits;
	}

	public class Operations
	{
		static readonly Logger _logger = LogManager.GetCurrentClassLogger();

		public const string PitPrefix = "PeachPit-";
		public string Username { get; set; }

		readonly string _password;
		readonly EnvironmentType _env;
		readonly NetworkCredential _creds;

		static readonly string BaseFeature = ConfigurationManager.AppSettings["OperationsBaseFeature"];

		public Operations(string user, string pass)
		{
			Username = user;
			_password = pass;
			_env = ConfigurationManager.AppSettings["OperationsEnvironment"] == "PROD" ? EnvironmentType.Production : EnvironmentType.UAT;
			_creds = new NetworkCredential(
				ConfigurationManager.AppSettings["OperationsServiceUser"],
				ConfigurationManager.AppSettings["OperationsServicePassword"]
			);
		}

		public bool ValidateCredentials()
		{
			return AcquireToken() != null;
		}

		public string AcquireToken()
		{
			try
			{
				var creds = new NetworkCredential(Username, _password);
				using (var service = Factory.Create<FlexnetAuthenticationService>(_env, creds))
				{
					var token = service.getSecureToken(new IdentityType { userId = Username });
					return token.token;
				}
			}
			catch (Exception ex)
			{
				_logger.Error(ex, "AcquireToken failed for user: {0}", Username);
				return null;
			}
		}

		public List<Activation> GetActivations()
		{
			// for testing multiple activations...
			//var fno = new FlexNetOperations(_env, _creds);
			//var user = fno.GetUser(Username);
			//Console.WriteLine("User: {0}", user.displayName);;
			//return new List<Activation>
			//{
			//	new Activation
			//	{
			//		Pits = new string[0],
			//		ActivationId = "3f83-c70e-e8c6-44db-8230-74c1-cca7-3dc7",
			//		OrgName = "Acme",
			//		LicenseServerUrl = "",
			//		Product = "Product",
			//	},
			//	new Activation
			//	{
			//		Pits = new string[0],
			//		ActivationId = "3f83-c70e-e8c6-44db-8230-74c1-cca7-3dc7",
			//		OrgName = "Acme",
			//		LicenseServerUrl = "",
			//		Product = "Product",
			//	},
			//};
			try
			{
				var fno = new FlexNetOperations(_env, _creds);
				var user = fno.GetUser(Username);
				var org = user.orgRolesList[0].organization;

				var activations = new List<Activation>();
				var entitlements = fno.GetEntitlements(org);
				foreach (var entitlement in entitlements)
				{
					var device = fno.GetLicenseServer(entitlement);
					var deviceUrl = string.Format(
						ConfigurationManager.AppSettings["OperationsLicenseServerUrl"],
						device.deviceIdentifier.deviceId
					);

					var candidates = new List<entitlementLineItemDataType>();
					var pits = new HashSet<string>();

					foreach (var item in entitlement.simpleEntitlement.lineItems)
					{
						var product = fno.GetProduct(item.product.primaryKeys.name, item.product.primaryKeys.version);
						foreach (var feature in product.features)
						{
							var name = feature.featureIdentifier.primaryKeys.name;
							if (name == BaseFeature)
								candidates.Add(item);
							else if (name.StartsWith(PitPrefix))
								pits.Add(name);
						}
					}

					var pitsArray = pits.ToArray();
					activations.AddRange(candidates.Select(
						x => new Activation
						{
							OrgName = org.primaryKeys.name,
							LicenseServerUrl = deviceUrl,
							ActivationId = x.activationId.id,
							Product = x.product.primaryKeys.name,
							Pits = pitsArray,
						}
					));
				}

				return activations;
			}
			catch (Exception ex)
			{
				_logger.Error(ex, "Exception in GetActivations for username: {0}", Username);
				return null;
			}
		}
	}
}
