using System;
using Lic = Peach.Pro.Core.License;

namespace Peach.Pro.Core.WebServices.Models
{
	public class License
	{
		/// <summary>
		/// Is the license falid.
		/// </summary>
		public bool IsValid { get; set; }

		/// <summary>
		/// Is the license invalid.
		/// </summary>
		public bool IsInvalid { get; set; }

		/// <summary>
		/// Is the license missing.
		/// </summary>
		public bool IsMissing { get; set; }

		/// <summary>
		/// Is the license expired.
		/// </summary>
		public bool IsExpired { get; set; }

		/// <summary>
		/// Human readable error for why license is not valid.
		/// </summary>
		public string ErrorText { get; set; }

		/// <summary>
		/// When the license expires.
		/// </summary>
		public DateTime Expiration { get; set; }

		/// <summary>
		/// The features associated with this license.
		/// </summary>
		public Lic.Feature Version { get; set; }

		/// <summary>
		/// Has the eula been accepted.
		/// </summary>
		public bool EulaAccepted { get; set; }
	}
}
