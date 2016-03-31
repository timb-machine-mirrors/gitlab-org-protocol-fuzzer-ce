/// <reference path="../reference.ts" />

namespace Peach {

	export class EulaService {
		static $inject = [
			C.Angular.$q,
			C.Angular.$http,
			C.Angular.$uibModal,
			C.Angular.$state
		];

		constructor(
			private $q: ng.IQService,
			private $http: ng.IHttpService,
			private $modal: ng.ui.bootstrap.IModalService,
			private $state: ng.ui.IStateService
		) {
		}

		public Verify(): ng.IPromise<ILicense> {
			return this.LoadLicense().then((l : ILicense) => {
				return this.VerifyLicense(l);
			});
		}

		private LoadLicense(): ng.IPromise<ILicense> {
			const promise = this.$http.get(C.Api.License);
			promise.catch((reason: ng.IHttpPromiseCallbackArg<IError>) => {
				this.$state.go(C.States.MainError, { message: reason.data.errorMessage });
			});
			return StripHttpPromise(this.$q, promise);
		}

		private VerifyLicense(license: ILicense): ng.IPromise<ILicense> {
			if (!license.isValid) {
				let title: string;

				if (license.isInvalid) {
					title = 'Invalid License Detected';
				} else if (license.isMissing) {
					title = 'Missing License Detected';
				} else if (license.isExpired) {
					title = 'Expired License Detected';
				} else {
					title = 'License Error Detected';
				}

				return this.LicenseError({
					Title: title,
					Body: license.errorText.split('\n')
				}).then(() => {
					return this.Verify();
				});
			}

			if (license.eulaAccepted) {
				const ret = this.$q.defer<ILicense>();
				ret.resolve(license);
				return ret.promise;
			}

			let template: string;
			let developer = false;

			switch (license.version) {
				case LicenseVersion.Enterprise:
				case LicenseVersion.Distributed:
					template = C.Templates.Eula.Enterprise;
					break;
				case LicenseVersion.ProfessionalWithConsulting:
				case LicenseVersion.Professional:
				case LicenseVersion.Developer:
					template = C.Templates.Eula.Professional;
					developer = true;
					break;
				case LicenseVersion.TrialAllPits:
				case LicenseVersion.Trial:
					template = C.Templates.Eula.Trial;
					break;
				case LicenseVersion.Academic:
					template = C.Templates.Eula.Acedemic;
					break;
				case LicenseVersion.TestSuites:
				case LicenseVersion.Studio:
				default:
					template = C.Templates.Eula.Professional;
					break;
			}

			let result = this.DisplayEula(template);

			if (developer) {
				return result.then(() => {
					return this.DisplayEula(C.Templates.Eula.Developer).then(() => {
						return this.AcceptEula();
					});
				});
			}

			return result.then(() => {
				return this.AcceptEula();
			});
		}

		private DisplayEula(url: string): ng.IPromise<any> {
			return this.$modal.open({
				templateUrl: url,
				controller: EulaController,
				controllerAs: C.ViewModel,
				backdrop: 'static',
				keyboard: false,
				size: 'lg'
			}).result;
		}

		private AcceptEula(): ng.IPromise<ILicense> {
			const promise = this.$http.post(C.Api.License, {});
			promise.then(() => {
				this.$state.reload();
			});
			promise.catch((reason: ng.IHttpPromiseCallbackArg<IError>) => {
				if (reason.status >= 500) {
					this.$state.go(C.States.MainError, { message: reason.data.errorMessage });
				}
			});
			return StripHttpPromise(this.$q, promise);
		}

		private LicenseError(options: ILicenseOptions) : ng.IPromise<any>
		{
			return this.$modal.open({
				templateUrl: C.Templates.Modal.License,
				controller: LicenseController,
				controllerAs: C.ViewModel,
				backdrop: 'static',
				keyboard: false,
				resolve: { Options: () => options }
			}).result;
		}
	}
}
