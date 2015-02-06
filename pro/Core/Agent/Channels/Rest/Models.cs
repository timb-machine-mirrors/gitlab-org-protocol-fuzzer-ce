using System.Collections.Generic;
using Newtonsoft.Json;

namespace Peach.Pro.Core.Agent.Channels.Rest
{
	internal class ConnectRequest
	{
		public class Monitor
		{
			[JsonProperty("name")]
			public string Name { get; set; }

			[JsonProperty("class")]
			public string Class { get; set; }

			[JsonProperty("args")]
			public Dictionary<string, string> Args { get; set; }
		}

		[JsonProperty("monitors")]
		public List<Monitor> Monitors { get; set; }
	}

	internal class IterationStartingRequest
	{
		[JsonProperty("isReproduction")]
		public bool IsReproduction { get; set; }

		[JsonProperty("lastWasFault")]
		public bool LastWasFault { get; set; }
	}

	internal class ConnectResponse
	{
		[JsonProperty("url")]
		public string Url { get; set; }

		[JsonProperty("messages")]
		public List<string> Messages { get; set; }
	}

	internal class BoolResponse
	{
		[JsonProperty("value")]
		public bool Value { get; set; }
	}

	internal class FaultResponse
	{
		public class Record
		{
			public class FaultDetail
			{
				[JsonProperty("description")]
				public string Description { get; set; }

				[JsonProperty("majorHash")]
				public string MajorHash { get; set; }

				[JsonProperty("minorHash")]
				public string MinorHash { get; set; }

				[JsonProperty("risk")]
				public string Risk { get; set; }

				[JsonProperty("mustStop")]
				public bool MustStop { get; set; }
			}

			public class FaultData
			{
				[JsonProperty("key")]
				public string Key { get; set; }

				[JsonProperty("size")]
				public int Size { get; set; }

				[JsonProperty("url")]
				public string Url { get; set; }
			}

			[JsonProperty("monitorName")]
			public string MonitorName { get; set; }

			[JsonProperty("detectionSource")]
			public string DetectionSource { get; set; }

			[JsonProperty("title")]
			public string Title { get; set; }

			[JsonProperty("fault")]
			public FaultDetail Fault { get; set; }

			[JsonProperty("data")]
			public List<FaultData> Data { get; set; }
		}

		[JsonProperty("faults")]
		public List<Record> Faults { get; set; }
	}

	internal class ExceptionResponse
	{
		[JsonProperty("message")]
		public string Message { get; set; }

		[JsonProperty("stackTrace")]
		public string StackTrace { get; set; }
	}
}
