using System.Web.Http;
using Peach.Pro.Core.WebServices.Models;
using Lic = Peach.Pro.Core.License;

namespace Peach.Pro.WebApi2.Controllers
{
	[RoutePrefix(Prefix)]
	public class LicenseController : ApiController
	{
		public const string Prefix = "p/license";

		[Route("")]
		public License Get()
		{
			return new License
			{
				IsValid = Lic.IsValid,
				IsInvalid = Lic.IsInvalid,
				IsMissing = Lic.IsMissing,
				IsExpired = Lic.IsExpired,
				ErrorText = Lic.ErrorText,
				Expiration = Lic.Expiration,
				Version = Lic.Version
			};
		}
	}
}
