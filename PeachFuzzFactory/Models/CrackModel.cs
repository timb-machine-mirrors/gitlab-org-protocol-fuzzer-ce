using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Peach.Core.Dom;
using Peach.Core.IO;
using PeachFuzzFactory.Controls;

namespace PeachFuzzFactory.Models
{
	public class CrackModel : Model, ITreeModel
	{
		public static CrackModel Root = null;

		public CrackModel(DataElement element, long startBits, long stopBits)
		{
			DataElement = element;
			StartBits = startBits;
			StopBits = stopBits;
			Error = false;
		}

		public string Name
		{
			get { return DataElement.name; }
		}

		public string IconName
		{
			get
			{
				if(Error)
					return "/Icons/node-error.png";

				return "/Icons/node-" + DataElement.GetType().Name.ToLower() + ".png";
			}
		}

		public DataElement DataElement
		{
			get;
			set;
		}

		public long StartBits
		{
			get;
			set;
		}

		public long StopBits
		{
			get;
			set;
		}

		public int Position
		{
			get { return (int)(StartBits / 8); }
		}

		public int Length
		{
			get { return StopBits == 0 ? 0 : (int)((StopBits - StartBits + 7) / 8); }
		}

		public bool Error
		{
			get;
			set;
		}

		public string Value
		{
			get
			{
				return DataElement.DefaultValue == null ? "" : DataElement.DefaultValue.ToString();
			}
		}

		protected ObservableCollection<CrackModel> children = new ObservableCollection<CrackModel>();
		public ObservableCollection<CrackModel> Children
		{
			get { return children; }
			set { children = value; }
		}

		#region ITreeModel Members

		public System.Collections.IEnumerable GetChildren(object parent)
		{
			if (parent == null)
				return new CrackModel[] { Root };

			return ((CrackModel)parent).Children;
		}

		public bool HasChildren(object parent)
		{
			if (parent == null)
				return true;

			return ((CrackModel)parent).Children.Count > 0;
		}

		#endregion
	}
}
