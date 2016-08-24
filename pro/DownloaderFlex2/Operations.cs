using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Web;
using FlexNet.Operations;
using NLog;

namespace PeachDownloader
{
	// TODO - Make the copy in servicefactory.cs public and use it instead.
	class Environment
	{
		public Dictionary<Type, string> ServiceUrls { get; set; }
	}

	public class Operations
	{
		static Logger logger = LogManager.GetCurrentClassLogger();

		private string Username { get; set; }
		private string Password { get; set; }
		private string AuthToken { get; set; }
		private EnvironmentType EnvironmentType { get; set; }
		private NetworkCredential Credentials { get; set; }
		private NetworkCredential ServiceCredentials { get; set; }

		private static readonly string BaseFeature = ConfigurationManager.AppSettings["OperationsBaseFeature"];

		// TODO - Make the copy in servicefactory.cs public and use it instead.
		private static readonly Dictionary<EnvironmentType, Environment> _envs = new Dictionary<EnvironmentType, Environment>
		{
			{
				EnvironmentType.Production,
				new Environment
				{
					ServiceUrls = new Dictionary<Type, string>()
					{
						{
							typeof(ProductPackagingService),
							"https://flex1253-fno.flexnetoperations.com/flexnet/services/ProductPackagingService"
						},
						{
							typeof(EntitlementOrderService),
							"http://flex1253-fno.flexnetoperations.com/flexnet/services/EntitlementOrderService"
						},
						{
							typeof(UserOrgHierarchyService),
							"https://flex1253-fno.flexnetoperations.com/flexnet/services/UserOrgHierarchyService"
						},
						{
							typeof(FlexnetAuthenticationService),
							"https://flex1253-fno.flexnetoperations.com/flexnet/services/FlexnetAuthentication"
						},
						{
							typeof(ManageDeviceService),
							"https://flex1253-fno.flexnetoperations.com/flexnet/services/ManageDeviceService"
						},
						{
							typeof(UsageService),
							"https://flex1253-fno.flexnetoperations.com/flexnet/services/UsageService"
						},
					}
				}
			},
			{
				EnvironmentType.UAT,
				new Environment
				{
					ServiceUrls = new Dictionary<Type, string>()
					{
						{
							typeof(ProductPackagingService),
							"https://flex1253-fno-uat.flexnetoperations.com/flexnet/services/ProductPackagingService"
						},
						{
							typeof(EntitlementOrderService),
							"http://flex1253-fno-uat.flexnetoperations.com/flexnet/services/EntitlementOrderService"
						},
						{
							typeof(UserOrgHierarchyService),
							"https://flex1253-fno-uat.flexnetoperations.com/flexnet/services/UserOrgHierarchyService"
						},
						{
							typeof(FlexnetAuthenticationService),
							"https://flex1253-fno-uat.flexnetoperations.com/flexnet/services/FlexnetAuthentication"
						},
						{
							typeof(ManageDeviceService),
							"https://flex1253-fno-uat.flexnetoperations.com/flexnet/services/ManageDeviceService"
						},
						{
							typeof(UsageService),
							"https://flex1253-fno-uat.flexnetoperations.com/flexnet/services/UsageService"
						},
					}
				}
			}
		};

		public Operations(string user, string pass)
		{
			Username = user;
			Password = pass;
			EnvironmentType = ConfigurationManager.AppSettings["OperationsEnvironment"] == "PROD" ? EnvironmentType.Production : EnvironmentType.UAT;
			Credentials = new NetworkCredential(Username, Password);
			ServiceCredentials = new NetworkCredential(
				ConfigurationManager.AppSettings["OperationsServiceUser"], 
				ConfigurationManager.AppSettings["OperationsServicePassword"]);
		}

		public bool ValidateCredentials()
		{
			try
			{
				using (var authService = new FlexnetAuthenticationService()
				{
					Url = _envs[EnvironmentType].ServiceUrls[typeof(FlexnetAuthenticationService)],
					Credentials = Credentials
				})
				{

					var identity = new IdentityType() {userId = Username};
					var token = authService.getSecureToken(identity);

					if (token.token == null)
						return false;

					AuthToken = token.token;

					return true;
				}
			}
			catch (Exception ex)
			{
				logger.Error(ex, "ValidateCredentials failed for user: {0}", Username);
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
				var orgName = string.Empty;
				var deviceUrl = string.Empty;
				var activations = new List<Activation>();

				using (var userOrgService = new UserOrgHierarchyService()
				{
					Url = _envs[EnvironmentType].ServiceUrls[typeof(UserOrgHierarchyService)],
					Credentials = ServiceCredentials
				})
				{
					var query = new getUsersQueryRequestType
					{
						batchSize = "5",
						pageNumber = "1",
						queryParams = new userQueryParametersType
						{
							canLogIn = true,
							isActive = true,
							userName = new SimpleQueryType {value = Username}
						}
					};

					// we should only get one

					var result = userOrgService.getUsersQuery(query);
					if (result.statusInfo.status != StatusType.SUCCESS)
					{
						logger.Error("getUsersQuery failed for user '{1}' with error: {0}", 
							result.statusInfo.reason,
							Username);

						return null;
					}

					if (result.responseData == null)
					{
						logger.Warn("getUsersQuery reault.responseData was null for user: {0}", Username);
						return null;
					}

					orgName = result.responseData[0].orgRolesList[0].organization.primaryKeys.name;
				}

				using (var deviceService = new ManageDeviceService()
				{
					Url = _envs[EnvironmentType].ServiceUrls[typeof(ManageDeviceService)],
					Credentials = ServiceCredentials
				})
				{
					var query = new getAutoProvisionedServerRequest { orgName = orgName };
					var result = deviceService.getAutoProvisionedServer(query);

					if (result.statusInfo.status != OpsEmbeddedStatusType.SUCCESS)
					{
						logger.Error("getAutoProvisionedServer failed for orgName '{1}' with error: {0}",
							result.statusInfo.reason,
							orgName);

						return null;
					}

					if (result.cloudLicenseServer == null)
					{
						logger.Warn("getAutoProvisionedServer reault.cloudLicenseServer was null for org: {0}", orgName);
					}
					else
					{
						deviceUrl = string.Format(ConfigurationManager.AppSettings["OperationsLicenseServerUrl"],
							result.cloudLicenseServer.deviceId);
					}
				}

				using (var entitleService = new EntitlementOrderService()
				{
					Url = _envs[EnvironmentType].ServiceUrls[typeof(EntitlementOrderService)],
					Credentials = ServiceCredentials
				})
				{
					var search = new searchEntitlementRequestType
					{
						batchSize = "5",
						pageNumber = "1",
						entitlementSearchCriteria = new searchEntitlementDataType
						{
							soldTo = new SimpleQueryType
							{
								searchType = simpleSearchType.EQUALS,
								value = orgName
							}
						}
					};

					var result = entitleService.getEntitlementsQuery(search);

					if (result.statusInfo.status != StatusType.SUCCESS)
					{
						logger.Error("getEntitlementsQuery failed for orgName '{1}' with error: {0}",
							result.statusInfo.reason,
							orgName);

						return null;
					}

					if (result.entitlement == null)
					{
						logger.Warn("getEntitlementsQuery reault.entitlement was null for org: {0}", orgName);
						return null;
					}

					foreach (var ent in result.entitlement)
					{
						foreach (var item in ent.simpleEntitlement.lineItems)
						{
							if (!HasBaseFeature(item.product.primaryKeys.name))
								continue;

							var act = new Activation
							{
								ActivationId = item.activationId.id,
								OrgName = ent.simpleEntitlement.soldTo,
								Product = item.product.primaryKeys.name,
								LicenseServerUrl = deviceUrl
							};

							activations.Add(act);
						}
					}

					return activations;
				}
			}
			catch (Exception ex)
			{
				logger.Error(ex, "Exception in ActivationIds for username: {0}", Username);
				return null;
			}
		}

		private bool HasBaseFeature(string productName)
		{
			try
			{
				using (var productService = new ProductPackagingService()
				{
					Url = _envs[EnvironmentType].ServiceUrls[typeof(ProductPackagingService)],
					Credentials = ServiceCredentials
				})
				{
					var query = new getProductsQueryRequestType
					{
						batchSize = "5",
						pageNumber = "1",
						returnContainedObjects = true,
						queryParams = new productQueryParametersType
						{
							productName = new SimpleQueryType
							{
								searchType = simpleSearchType.EQUALS,
								value = productName
							}
						}
					};

					var result = productService.getProductsQuery(query);
					
					if (result.statusInfo.status != StatusType.SUCCESS)
					{
						logger.Error("getProductsQuery failed for productName '{1}' with error: {0}",
							result.statusInfo.reason,
							productName);

						throw new ArgumentException("HasBaseFeature, request failed.");
					}

					if (result.responseData == null)
					{
						logger.Warn("getProductsQuery reault.responseData was null for productName: {0}", productName);
						throw new ArgumentException(string.Format("HasBaseFeature, unable to find productName: {0}", productName));
					}

					var feature =
						result.responseData[0].features.FirstOrDefault(f => f.featureIdentifier.primaryKeys.name == BaseFeature);

					return feature != null;
				}
			}
			catch (Exception ex)
			{
				logger.Error(ex, "Exception in HasBaseFeature for productName: {0}", productName);
				throw;
			}
		}
	}
}