using System;
using System.Collections.Generic;

namespace Peach.Enterprise.WebServices.Models
{
	public class LibraryVersion
	{
		public uint Version { get; set; }

		public bool Locked { get; set; }

		public List<LibraryPit> Pits { get; set; }
	}
}
