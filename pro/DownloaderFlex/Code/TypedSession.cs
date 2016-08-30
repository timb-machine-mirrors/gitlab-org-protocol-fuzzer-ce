using System.Web;

namespace PeachDownloader
{
	public class TypedSession<Subclass> where Subclass : TypedSession<Subclass>, new()
	{
		static string Key
		{
			get { return typeof(TypedSession<Subclass>).FullName; }
		}

		static Subclass Value
		{
			get { return (Subclass)HttpContext.Current.Session[Key]; }
			set { HttpContext.Current.Session[Key] = value; }
		}

		public static Subclass Current
		{
			get
			{
				var instance = Value;
				if (instance == null)
				{
					lock (HttpContext.Current.Session.SyncRoot)
					{
						// standard lock double-check
						instance = Value;
						if (instance == null)
							Value = instance = new Subclass();
					}
				}
				return instance;
			}
		}
	}
}
