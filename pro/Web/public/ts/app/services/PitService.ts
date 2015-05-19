/// <reference path="../reference.ts" />

module Peach {
	"use strict";

	export class PitService {

		static $inject = [
			C.Angular.$q,
			C.Angular.$http,
			C.Angular.$state
		];

		constructor(
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
			return StripHttpPromise(this.$q, promise);
		}

		public LoadPeachMonitors(): ng.IPromise<IMonitor[]> {
			return StripHttpPromise(this.$q, this.$http.get(C.Api.PeachMonitors));
		}

		public LoadPit(): ng.IPromise<IPit> {
			var url = C.Api.PitUrl.replace(':id', this.CurrentPitId);
			var promise = this.$http.get(url);
			promise.success((pit: IPit) => this.OnSuccess(pit));
			return StripHttpPromise(this.$q, promise);
		}

		public SavePit(): ng.IPromise<IPit> {
			var promise = this.$http.post(this.pit.pitUrl, this.pit);
			promise.success((pit: IPit) => this.OnSuccess(pit));
			return StripHttpPromise(this.$q, promise);
		}
		
		public SaveConfig(config: IParameter[]): ng.IPromise<IPit> {
			this.pit.config = config;
			return this.SavePit();
		}
		
		public SaveAgents(agents: Agent[]): ng.IPromise<IPit> {
			this.pit.agents = agents;
			return this.SavePit();
		}

		public SaveTemplate(): ng.IPromise<IPit> {
			var request: IPitCopy = {
				libraryUrl: this.userPitLibrary,
				pitUrl: this.pit.pitUrl,
				name: this.pit.name,
				description: this.pit.description
			}
			var promise = this.$http.post(C.Api.Pits, request);
			promise.success((pit: IPit) => this.OnSuccess(pit));
			return StripHttpPromise(this.$q, promise);
		}

		public get IsConfigured(): boolean {
			return onlyIf(this.pit, () => _.last(this.pit.versions).configured) || false;
		}
		
		private OnSuccess(pit: IPit) {
			this.pit = pit;
		}
	}
}
