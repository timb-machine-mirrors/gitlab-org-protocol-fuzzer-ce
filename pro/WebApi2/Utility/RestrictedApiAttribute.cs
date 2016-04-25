using System.Net;
using System.Net.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using Peach.Pro.Core;

namespace Peach.Pro.WebApi2.Utility
{
	internal class RestrictedApiAttribute : ActionFilterAttribute
	{
		public override void OnActionExecuting(HttpActionContext context)
		{
			var resolver = context.RequestContext.Configuration.DependencyResolver; 
			var license = resolver.GetService(typeof(ILicense)) as ILicense;

			if (!license.IsValid)
			{
				context.Response = new HttpResponseMessage(HttpStatusCode.PaymentRequired);
			}
			else if (!license.EulaAccepted)
			{
				context.Response = new HttpResponseMessage(HttpStatusCode.Unauthorized);
			}
			else
			{
				base.OnActionExecuting(context);
			}
		}
	}
}
