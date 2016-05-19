using System;

namespace Peach.Pro.Core.License
{
	public enum LicenseFeature
	{
		Enterprise,
		Distributed,
		ProfessionalWithConsulting,
		Professional,
		TrialAllPits,
		Trial,
		Academic,
		TestSuites,
		Studio,
		Developer,
		Unknown,
	}

	public interface ILicense
	{
		string ErrorText { get; }
		LicenseFeature Version { get; }
		DateTime Expiration { get; }
		string ExpirationWarning { get; }
		bool IsMissing { get; }
		bool IsExpired { get; }
		bool IsInvalid { get; }
		bool IsNearingExpiration { get; }
		int ExpirationInDays { get; }
		bool IsValid { get; }
		bool EulaAccepted { get; set; }
		string EulaText();
		string EulaText(LicenseFeature version);

		//IEnumerable<IFeature> GetFeatures();
		IFeature GetFeature(string name);
	}

	public interface IFeature : IDisposable
	{
		string Name { get; }
		byte[] Key { get; }

		bool Acquire();
		void Release();
	}
}
