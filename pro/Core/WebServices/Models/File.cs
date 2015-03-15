using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations.Schema;
namespace Peach.Pro.Core.WebServices.Models
{
	public class FaultFile
	{
		/// <summary>
		/// Unique ID of the file.
		/// </summary>
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public long Id { get; set; }

		/// <summary>
		/// Foreign key to FaultDetail table
		/// </summary>
		[JsonIgnore]
		public long FaultDetailId { get; set; }

		/// <summary>
		///  The name of the file.
		/// </summary>
		/// <example>
		/// "WinAgent.Monitor.WindowsDebugEngine.description.txt"
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
		[NotMapped]
		public string FileUrl { get; set; }

		/// <summary>
		/// The size of the contents of the file.
		/// </summary>
		/// <example>
		/// 1024
		/// </example>
		public long Size { get; set; }
	}
}
