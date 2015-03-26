using System;
using System.Collections.Generic;
using System.Linq;
using Nancy;
using Nancy.ModelBinding;
using Peach.Pro.Core.WebServices.Models;

namespace Peach.Pro.Core.WebServices
{
	public class PitService : WebService
	{
		public static readonly string Prefix = "/p/pits";

		public PitService(WebContext context)
			: base(context, Prefix)
		{
			Get[""] = _ => GetPits();
			Post[""] = _ => CopyPit();

			Get["/{id}"] = _ => GetPit(_.id);
			Post["/{id}"] = _ => PostPit(_.id);
		}

		Response GetPits()
		{
			return Response.AsJson(PitDatabase.Entries.ToArray());
		}

		Response GetPit(string id)
		{
			var pit = PitDatabase.GetPitById(id);
			if (pit == null)
				return HttpStatusCode.NotFound;
			pit.Config = PitDatabase.GetConfigById(id);
			pit.Agents = PitDatabase.GetAgentsById(id);
			pit.Calls = PitDatabase.GetCallsById(id);
			pit.PeachConfig = new List<KeyValuePair<string, string>>
			{
				new KeyValuePair<string, string>(
					"LocalOS", 
					Peach.Core.Platform.GetOS().ToString().ToLower()
				)
			};
			return Response.AsJson(pit);
		}

		Response CopyPit()
		{
			var data = this.Bind<PitCopy>();
			var newUrl = "";

			try
			{
				newUrl = PitDatabase.CopyPit(
					data.LibraryUrl, 
					data.PitUrl, 
					data.Name, 
					data.Description
				);
			}
			catch (KeyNotFoundException)
			{
				return HttpStatusCode.NotFound;
			}
			catch (UnauthorizedAccessException)
			{
				return HttpStatusCode.Forbidden;
			}
			catch (ArgumentException)
			{
				return HttpStatusCode.BadRequest;
			}

			var pit = PitDatabase.GetPitByUrl(newUrl);
			pit.Config = PitDatabase.GetConfigByUrl(newUrl);
			pit.Agents = PitDatabase.GetAgentsByUrl(newUrl);
			pit.Calls = PitDatabase.GetCallsByUrl(newUrl);
			return Response.AsJson(pit);
		}

		Response PostPit(string id)
		{
			var pit = PitDatabase.GetPitById(id);
			if (pit == null)
				return HttpStatusCode.NotFound;

			// Don't allow changing configuration values
			// of locked pits
			if (pit.Locked)
				return HttpStatusCode.Forbidden;

			var data = this.Bind<Pit>();
			if (data.Config != null)
				PitDatabase.SaveConfig(pit, data.Config);
			if (data.Agents != null)
				PitDatabase.SaveAgents(pit, data.Agents);
			return GetPit(id);
		}
	}
}
