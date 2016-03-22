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
			if (!License.IsValid)
			{
				context.Response = new HttpResponseMessage(HttpStatusCode.PaymentRequired);
			}
			else if (!License.EulaAccepted)
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
