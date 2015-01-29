using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.ComponentModel;
using Peach.Core.IO;

namespace Peach.Core.Dom.Actions
{
	[Action("GetProperty")]
	[Serializable]
	public class GetProperty : Action
	{
		/// <summary>
		/// Property to operate on
		/// </summary>
		[XmlAttribute]
		[DefaultValue(null)]
		public string property { get; set; }

		/// <summary>
		/// Data model to populate with property value
		/// </summary>
		public ActionData data { get; set; }

		public override IEnumerable<ActionData> allData
		{
			get
			{
				yield return data;
			}
		}

		public override IEnumerable<BitwiseStream> inputData
		{
			get
			{
				yield break;
			}
		}

		protected override void OnRun(Publisher publisher, RunContext context)
		{
			publisher.start();

			var result = publisher.getProperty(property);
			data.dataModel.DefaultValue = result;
		}
	}
}
