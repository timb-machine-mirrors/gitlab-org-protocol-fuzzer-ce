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
			return this.LoadLicense().then(license => {
				return this.VerifyLicense(license);
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

			let promise: ng.IPromise<ILicense> = null;
			for (var type of license.eulas) {
				if (!promise) {
					promise = this.DisplayEula(type);
				} else {
					promise = promise.then(() => {
						return this.DisplayEula(type);
					});
				}
			}
			return promise.then(() => {
				return this.AcceptEula()
			})
		}

		private DisplayEula(type: string): ng.IPromise<any> {
			return this.$modal.open({
				templateUrl: `html/eula/${type}.html`,
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
