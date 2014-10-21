using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nancy;

namespace Peach.Core.Agent.Channels.RestServer
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
