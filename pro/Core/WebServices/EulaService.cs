using Nancy;
using Peach.Core;

namespace Peach.Pro.Core.WebServices
{
	public class EulaService : NancyModule
	{
		public EulaService()
			: base("")
		{
			Get["/eula"] = _ =>
			{
				if (License.EulaAccepted)
					return Response.AsRedirect("/");

				return View["Eula", new
				{
					Rejected = false,
					Version = License.Version.ToString(),
					EulaText = License.EulaText()
				}];
			};

			Post["/eula", ctx => Accepted(Eula(ctx))] = _ =>
			{
				License.EulaAccepted = true;

				return Response.AsRedirect("/eula");
			};

			Post["/eula", ctx => Rejected(Eula(ctx))] = _ => View["Eula", new { Rejected = true }];

			Post["/eula", ctx => NotFound(Eula(ctx))] = _ => Response.AsRedirect("/eula");
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
