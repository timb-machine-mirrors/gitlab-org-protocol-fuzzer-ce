using System;
using System.Collections.Generic;
using Peach.Core;

namespace Peach.Pro.Core.License
{
	public enum EulaType
	{
		Academic,
		Developer,
		Enterprise,
		Professional,
		Trial,
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

		IEnumerable<string> AvailablePits { get; }
		byte[] CanUsePit(string name);
		bool CanUseCustomPit();
		IJobLicense NewJob(string pit, string config, string job);
	}

	public interface IJobLicense : IDisposable
	{
		bool CanExecuteTestCase();
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
