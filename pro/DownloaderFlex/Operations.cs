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
	public class Operations
	{
		static Logger _logger = LogManager.GetCurrentClassLogger();

		private string _username;
		private string _password;
		private EnvironmentType _env;
		private readonly NetworkCredential _creds;

		private static readonly string BaseFeature = ConfigurationManager.AppSettings["OperationsBaseFeature"];

		public Operations(string user, string pass)
		{
			_username = user;
			_password = pass;
			_env = ConfigurationManager.AppSettings["OperationsEnvironment"] == "PROD" ? EnvironmentType.Production : EnvironmentType.UAT;
			_creds = new NetworkCredential(
				ConfigurationManager.AppSettings["OperationsServiceUser"],
				ConfigurationManager.AppSettings["OperationsServicePassword"]
			);
		}

		public bool ValidateCredentials()
		{
			try
			{
				var creds = new NetworkCredential(_username, _password);
				using (var service = Factory.Create<FlexnetAuthenticationService>(_env, creds))
				{
					var token = service.getSecureToken(new IdentityType { userId = _username });
					if (token.token == null)
						return false;

					//_authToken = token.token;
					return true;
				}
			}
			catch (Exception ex)
			{
				_logger.Error(ex, "ValidateCredentials failed for user: {0}", _username);
				return false;
			}
		}

		public class Activation
		{
			public string OrgName;
			public string ActivationId;
			public string Product;
			public string LicenseServerUrl;
		}

		public List<Activation> ActivationIds()
		{
			try
			{
				var fno = new FlexNetOperations(_env, _creds);
				var user = fno.GetUser(_username);
				var org = user.orgRolesList[0].organization;

				var activations = new List<Activation>();
				var entitlements = fno.GetEntitlements(org);
				foreach (var entitlement in entitlements)
				{
					var device = fno.GetDevice(entitlement);
					var deviceUrl = string.Format(
						ConfigurationManager.AppSettings["OperationsLicenseServerUrl"],
						device.deviceIdentifier.deviceId
					);

					foreach (var item in entitlement.simpleEntitlement.lineItems)
					{
						var product = fno.GetProduct(item.product.primaryKeys.name, item.product.primaryKeys.version);
						if (!product.features.Any(x => x.featureIdentifier.primaryKeys.name == BaseFeature))
							continue;

						var act = new Activation
						{
							ActivationId = item.activationId.id,
							OrgName = entitlement.simpleEntitlement.soldTo,
							Product = item.product.primaryKeys.name,
							LicenseServerUrl = deviceUrl
						};

						activations.Add(act);
					}
				}

				return activations;
			}
			catch (Exception ex)
			{
				_logger.Error(ex, "Exception in ActivationIds for username: {0}", _username);
				return null;
			}
		}
	}
}
