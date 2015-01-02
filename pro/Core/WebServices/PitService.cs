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
			Get["/{id}"] = _ => GetPit(_.id);
			Get["/{id}/monitors"] = _ => GetPitMonitors(_.id);

			Get["/{id}/config"] = _ => GetPitConfig(_.id);
			Post["/{id}/config"] = _ => PostPitConfig(_.id);

			Get["/{id}/agents"] = _ => GetPitAgents(_.id);
			Post["/{id}/agents"] = _ => PostPitAgents(_.id);

			Post[""] = _ => CopyPit();
		}

		object GetPits()
		{
			return PitDatabase.Entries.ToArray();
		}

		object GetPit(string id)
		{
			var pit = PitDatabase.GetPitById(id);
			if (pit == null)
				return HttpStatusCode.NotFound;

			return pit;
		}

		object GetPitConfig(string id)
		{
			var cfg = PitDatabase.GetConfigById(id);
			if (cfg == null)
				return HttpStatusCode.NotFound;
			return cfg;
		}

		object GetPitAgents(string id)
		{
			var agents = PitDatabase.GetAgentsById(id);
			if (agents == null)
				return HttpStatusCode.NotFound;
			return agents;
		}

		object GetPitMonitors(string id)
		{
			var monitors = PitDatabase.GetAllMonitors(id);
			if (monitors == null)
				return HttpStatusCode.NotFound;
			return monitors;
		}

		object CopyPit()
		{
			var data = this.Bind<PitCopy>();
			var db = PitDatabase;
			var newUrl = "";

			try
			{
				newUrl = db.CopyPit(data.LibraryUrl, data.Pit.PitUrl, data.Pit.Name, data.Pit.Description);
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

			var newPit = db.GetPitByUrl(newUrl);

			return newPit;
		}

		object PostPitConfig(string id)
		{
			var pit = PitDatabase.GetPitById(id);
			if (pit == null)
				return HttpStatusCode.NotFound;

			// Don't allow changing configuration values
			// of locked pits
			if (pit.Locked)
				return HttpStatusCode.Forbidden;

			var data = this.Bind<PitConfig>();
			PitDatabase.SaveConfig(pit, data.Config);
			return data;
		}

		object PostPitAgents(string id)
		{
			var pit = PitDatabase.GetPitById(id);
			if (pit == null)
				return HttpStatusCode.NotFound;
	
			// Don't allow changing configuration values
			// of locked pits
			if (pit.Locked)
				return HttpStatusCode.Forbidden;

			var data = this.Bind<PitAgents>();
			PitDatabase.SaveAgents(pit, data.Agents);
			return data;
		}
	}
}
