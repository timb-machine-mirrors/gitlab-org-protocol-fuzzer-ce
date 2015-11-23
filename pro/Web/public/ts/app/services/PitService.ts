/// <reference path="../reference.ts" />

namespace Peach {
	"use strict";

	export class PitService {

		static $inject = [
			C.Angular.$rootScope,
			C.Angular.$q,
			C.Angular.$http,
			C.Angular.$state
		];

		constructor(
			private $rootScope: ng.IRootScopeService,
			private $q: ng.IQService,
			private $http: ng.IHttpService,
			private $state: ng.ui.IStateService
		) {
		}

		private pit: IPit;
		private userPitLibrary: string;

		public get CurrentPitId() {
			return this.$state.params['pit'];
		}

		public get Pit(): IPit {
			return this.pit;
		}

		public LoadLibrary(): ng.IPromise<ILibrary[]> {
			var promise = this.$http.get(C.Api.Libraries);
			promise.success((libs: ILibrary[]) => {
				this.userPitLibrary = _(libs)
					.reject({ locked: true })
					.first()
					.libraryUrl;
			});
			promise.catch((reason: ng.IHttpPromiseCallbackArg<IError>) => {
				this.$state.go(C.States.MainError, { message: reason.data.errorMessage });
			});
			return StripHttpPromise(this.$q, promise);
		}

		public LoadPeachMonitors(): ng.IPromise<IMonitor[]> {
			return StripHttpPromise(this.$q, this.$http.get(C.Api.PeachMonitors));
		}

		public LoadPit(): ng.IPromise<IPit> {
			var url = C.Api.PitUrl.replace(':id', this.CurrentPitId);
			var promise = this.$http.get(url);
			promise.success((pit: IPit) => this.OnSuccess(pit, false));
			promise.catch((reason: ng.IHttpPromiseCallbackArg<IError>) => {
				this.$state.go(C.States.MainError, { message: reason.data.errorMessage });
			});
			return StripHttpPromise(this.$q, promise);
		}

		public SavePit(): ng.IPromise<IPit> {
			var promise = this.$http.post(this.pit.pitUrl, this.pit);
			promise.success((pit: IPit) => this.OnSuccess(pit, true));
			return StripHttpPromise(this.$q, promise);
		}

		public SaveVars(config: IParameter[]): ng.IPromise<IPit> {
			this.pit.config = config;
			return this.SavePit();
		}

		public SaveAgents(agents: Agent[]): ng.IPromise<IPit> {
			this.pit.agents = agents;
			return this.SavePit();
		}

		public SaveConfig(pit: IPit): ng.IHttpPromise<IPit> {
			var request: IPitCopy = {
				libraryUrl: this.userPitLibrary,
				pitUrl: pit.pitUrl,
				name: pit.name,
				description: pit.description
			}
			var promise = this.$http.post(C.Api.Pits, request);
			promise.success((pit: IPit) => this.OnSuccess(pit, true));
			return promise;
		}

		public get IsConfigured(): boolean {
			return onlyIf(this.pit, () => _.all(this.pit.config, (c: IParameter) => {
				return c.optional || c.value !== "";
			}));
		}

		public get HasMonitors(): boolean {
			return onlyIf(this.pit, () => _.any(this.pit.agents, (a: Agent) => {
				return a.monitors.length > 0;
			}));
		}

		private OnSuccess(pit: IPit, saved: boolean) {
			var oldPit = this.pit;
			this.pit = pit;
			this.$rootScope['pit'] = pit;
			if (saved || (oldPit && oldPit.id !== pit.id)) {
				this.$rootScope.$emit(C.Events.PitChanged, pit);
			}
		}
	}
}
