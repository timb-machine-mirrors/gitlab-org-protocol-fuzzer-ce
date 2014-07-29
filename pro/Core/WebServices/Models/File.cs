using System;

namespace Peach.Enterprise.WebServices.Models
{
	public class File
	{
		/// <summary>
		///  The name of the file.
		/// </summary>
		/// <example>
		/// "Faults/PROBABLY_EXPLOITABLE_0x63103514_0x32621b6f/13/WinAgent.Monitor.WindowsDebugEngine.description.txt"
		/// </example>
		public string Name { get; set; }

		/// <summary>
		///  The full name of the file including path.
		/// </summary>
		/// <example>
		/// "Faults/PROBABLY_EXPLOITABLE_0x63103514_0x32621b6f/13/WinAgent.Monitor.WindowsDebugEngine.description.txt"
		/// </example>
		public string FullName { get; set; }

		/// <summary>
		/// The location to download the contents of the file.
		/// </summary>
		/// <example>
		/// "/p/files/{guid}"
		/// </example>
		public string FileUrl { get; set; }
	}
}
