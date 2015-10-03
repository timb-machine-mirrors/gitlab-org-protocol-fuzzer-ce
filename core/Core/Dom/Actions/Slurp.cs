using System;
using System.Collections.Generic;
using Peach.Core.Dom.XPath;
using System.Xml.Serialization;
using System.ComponentModel;

namespace Peach.Core.Dom.Actions
{
	[Action("Slurp")]
	[Serializable]
	public class Slurp : Action
	{
		/// <summary>
		/// xpath for selecting set targets during slurp.
		/// </summary>
		/// <remarks>
		/// Can return multiple elements.  All returned elements
		/// will be updated with a new value.
		/// </remarks>
		[XmlAttribute]
		[DefaultValue(null)]
		public string setXpath { get; set; }

		/// <summary>
		/// xpath for selecting value during slurp
		/// </summary>
		/// <remarks>
		/// Must return a single element.
		/// </remarks>
		[XmlAttribute]
		[DefaultValue(null)]
		public string valueXpath { get; set; }

		protected override void OnRun(Publisher publisher, RunContext context)
		{
			var resolver = new PeachXmlNamespaceResolver();
			var navi = new PeachXPathNavigator(parent.parent);
			var iter = navi.Select(valueXpath, resolver);

			var elems = new List<DataElement>();

			while (iter.MoveNext())
			{
				var valueElement = ((PeachXPathNavigator)iter.Current).CurrentNode as DataElement;
				if (valueElement == null)
					throw new SoftException("Error, slurp valueXpath did not return a Data Element. [" + valueXpath + "]");

				if (valueElement.InScope())
					elems.Add(valueElement);
			}

			if (elems.Count == 0)
				throw new SoftException("Error, slurp valueXpath returned no values. [" + valueXpath + "]");

			if (elems.Count != 1)
				throw new SoftException("Error, slurp valueXpath returned multiple values. [" + valueXpath + "]");

			iter = navi.Select(setXpath, resolver);

			if (!iter.MoveNext())
				throw new SoftException("Error, slurp setXpath returned no values. [" + setXpath + "]");

			do
			{
				var setElement = ((PeachXPathNavigator)iter.Current).CurrentNode as DataElement;
				if (setElement == null)
					throw new PeachException("Error, slurp setXpath did not return a Data Element. [" + valueXpath + "]");

				logger.Debug("Slurp, setting {0} from {1}", setElement.fullName, elems[0].fullName);
				setElement.DefaultValue = elems[0].DefaultValue;
			}
			while (iter.MoveNext());
		}
	}
}
