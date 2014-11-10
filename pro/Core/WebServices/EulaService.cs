using Nancy;
using Nancy.Responses;
using System;

namespace Peach.Enterprise.WebServices
{
	public class EulaService : NancyModule
	{
		public EulaService()
			: base("")
		{
			Get["/eula"] = _ =>
			{
				if (Peach.Core.License.EulaAccepted)
					return Response.AsRedirect("/app/index.html");

				return View["Eula", new
				{
					Rejected = false,
					Version = Peach.Core.License.Version,
					EulaText = Peach.Core.License.EulaText()
				}];
			};

			Post["/eula", (ctx) => Accepted(Eula(ctx))] = _ =>
			{
				Peach.Core.License.EulaAccepted = true;

				return Response.AsRedirect("/eula");
			};

			Post["/eula", (ctx) => Rejected(Eula(ctx))] = _ =>
			{
				return View["Eula", new { Rejected = true }];
			};

			Post["/eula", (ctx) => NotFound(Eula(ctx))] = _ =>
			{
				return Response.AsRedirect("/eula");
			};
		}

		static bool? Eula(NancyContext ctx)
		{
			var val = ctx.Request.Form.accept.Default<string>(null);

			bool accepted;
			if (!bool.TryParse(val, out accepted))
				return null;

			return accepted;
		}

		static bool Accepted(bool? eula)
		{
			return eula.HasValue && eula.Value;
		}

		static bool Rejected(bool? eula)
		{
			return eula.HasValue && !eula.Value;
		}

		static bool NotFound(bool? eula)
		{
			return !eula.HasValue;
		}
	}
}
