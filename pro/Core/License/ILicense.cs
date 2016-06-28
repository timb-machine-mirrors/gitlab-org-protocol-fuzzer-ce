using System;
using System.Collections.Generic;
using Peach.Core;

namespace Peach.Pro.Core.License
{
	public enum EulaType
	{
		Acedemic,
		Developer,
		Enterprise,
		Professional,
		Trial,
	}

	public static class FeatureNames
	{
		public const string Android = "Peach-Android";
		public const string Engine = "Peach-Engine";
		public const string Edition = "Peach-Edition";
		public const string ExportPit = "Peach-ExportPit";
		public const string CustomPit = "Peach-CustomPit";
	}

	public interface ILicense
	{
		bool IsMissing { get; }
		bool IsExpired { get; }
		bool IsInvalid { get; }
		bool IsValid { get; }
		string ErrorText { get; }
		DateTime Expiration { get; }

		bool EulaAccepted { get; set; }
		string EulaText { get; }
		IEnumerable<EulaType> Eulas { get; }

		IEnumerable<IFeature> Features { get; }

		/// <summary>
		/// Get a feature by name.
		/// <list type="bullet">
		/// <item>Peach-Android</item>
		/// <item>Peach-Engine</item>
		/// <item>Peach-ExportPit</item>
		/// <item>PeachPit-Custom</item>
		/// <item>PeachPit-$CATEGORY-$PIT</item>
		/// </list>
		/// </summary>
		/// <returns>The feature.</returns>
		/// <param name="name">Feature name.</param>
		IFeature GetFeature(string name);
	}

	public interface IFeature : IDisposable
	{
		string Name { get; }
		byte[] Key { get; }

		bool Acquire();
		// Call Dispose() to release the license
	}

	public static class LicenseExtensions
	{
		public static string ExpirationWarning(this ILicense license)
		{
			return "Warning: Peach expires in {0} days".Fmt(license.ExpirationInDays());
		}

		public static int ExpirationInDays(this ILicense license)
		{
			return (license.Expiration - DateTime.Now).Days;
		}

		public static bool IsNearingExpiration(this ILicense license)
		{
			return license.IsValid && license.Expiration < DateTime.Now.AddDays(30);
		}
	}
}
