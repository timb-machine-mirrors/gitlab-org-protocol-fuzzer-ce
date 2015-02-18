using System;
using Nancy;

#if DISABLED

namespace Peach.Pro.Core.Agent.Channels.RestServer
{

	public abstract class RestService : NancyModule
	{
		protected RestContext context;

		public RestService(RestContext context)
			: this(context, String.Empty)
		{
		}

		public RestService(RestContext context, string modulePath)
			: base(modulePath)
		{
			this.context = context;
		}

	}
}
#endif
