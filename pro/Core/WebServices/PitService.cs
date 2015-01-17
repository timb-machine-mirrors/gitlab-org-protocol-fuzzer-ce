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

			// Deprecated
			Get["/{id}/config"] = _ => GetPitConfig(_.id);
			// Deprecated
			Post["/{id}/config"] = _ => PostPitConfig(_.id);

			// Deprecated
			Get["/{id}/agents"] = _ => GetPitAgents(_.id);
			// Deprecated
			Post["/{id}/agents"] = _ => PostPitAgents(_.id);

			// Deprecated
			Get["/{id}/calls"] = _ => GetPitCalls(_.id);
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
			return pit;
		}

		// Deprecated
		object GetPitConfig(string id)
		{
			var ret = new PitConfig
			{
				PitUrl = PitService.Prefix + "/" + id,
				Config = PitDatabase.GetConfigById(id),
			};
			if (ret.Config == null)
				return HttpStatusCode.NotFound;
			return ret;
		}

		// Deprecated
		object GetPitAgents(string id)
		{
			var ret = new PitAgents
			{
				PitUrl = PitService.Prefix + "/" + id,
				Agents = PitDatabase.GetAgentsById(id),
			};
			if (ret.Agents == null)
				return HttpStatusCode.NotFound;
			return ret;
		}

		// Deprecated
		object GetPitCalls(string id)
		{
			var calls = PitDatabase.GetCallsById(id);
			if (calls == null)
				return HttpStatusCode.NotFound;
			return calls;
		}

		object CopyPit()
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
			return pit;
		}

		// Deprecated
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

		// Deprecated
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

		object PostPit(string id)
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
