/// <reference path="../reference.ts" />

module Peach {
	"use strict";

	export class PitService {

		static $inject = [
			C.Angular.$q,
			C.Angular.$window,
			C.Angular.$modal,
			C.Angular.$http
		];

		constructor(
			private $q: ng.IQService,
			private $window: ng.IWindowService,
			private $modal: ng.ui.bootstrap.IModalService,
			private $http: ng.IHttpService
		) {
		}

		private pendingPit: ng.IDeferred<IPit>;

		private userPitLibrary: string;
		public get UserPitLibrary(): string {
			return this.userPitLibrary;
		}

		private pit: IPit;
		public get Pit(): IPit {
			return this.pit;
		}

		private changeHandlers: Function[] = [];
		public OnPitChanged(callback: Function) {
			this.changeHandlers.push(callback);
		}

		private notifyOnPitChanged() {
			this.changeHandlers.forEach(fn => fn());
		}

		public LoadLibrary(): ng.IPromise<ILibrary[]> {
			var promise = this.$http.get(C.Api.Libraries);
			promise.success((libs: ILibrary[]) => {
				this.userPitLibrary = _.chain(libs)
					.reject('locked')
					.first()
					.libraryUrl;
			});
			return StripHttpPromise(this.$q, promise);
		}

		public LoadPeachMonitors(): ng.IPromise<IMonitor[]> {
			var promise = this.$http.get(C.Api.PeachMonitors);
			return StripHttpPromise(this.$q, promise);
		}

		// MainController 
		// -> PitLibraryController (modal)
		//    -> SelectPit 
		//       -> CopyPitController (modal)
		//          -> CopyPit
		public SelectPit(url: string): ng.IPromise<IPit> {
			this.pendingPit = this.$q.defer<IPit>();
			var promise = this.$http.get(url);
			promise.success((pit: IPit) => {
				if (pit.locked) {
					var modal = this.$modal.open({
						templateUrl: C.Templates.Modal.CopyPit,
						controller: CopyPitController,
						resolve: { pit: () => pit }
					});
					modal.result.then((copied: IPit) => {
						// only update the current Pit if successful
						// a failed copy leaves the current Pit untouched
						this.changePit(copied);
						this.pendingPit.resolve(this.pit);
					});
					modal.result.catch((reason) => {
						this.pendingPit.reject(reason);
					});
				} else {
					this.changePit(pit);
					this.pendingPit.resolve(this.pit);
				}
			});
			promise.error(reason => {
				this.pendingPit.reject(reason);
			});
			return this.pendingPit.promise;
		}

		private changePit(pit: IPit) {
			this.pit = pit;
			this.$window.sessionStorage.setItem('pitUrl', pit.pitUrl);
			this.notifyOnPitChanged();
		}

		public RestorePit(): boolean {
			var pitUrl = this.$window.sessionStorage.getItem('pitUrl');
			if (_.isString(pitUrl)) {
				this.SelectPit(pitUrl);
				return true;
			}
			return false;
		}

		public ReloadPit(): ng.IPromise<IPit> {
			if (_.isUndefined(this.pit)) {
				if (this.pendingPit) {
					return this.pendingPit.promise;
				}
				return this.$q.reject('no pit selected');
			}

			var promise = this.$http.get(this.pit.pitUrl);
			promise.success((pit: IPit) => {
				this.pit = pit;
			});
			return StripHttpPromise(this.$q, promise);
		}

		public SavePit(): ng.IPromise<IPit> {
			if (_.isUndefined(this.pit)) {
				return this.$q.reject('no pit selected');
			}

			var promise = this.$http.post(this.pit.pitUrl, this.pit);
			promise.success((pit: IPit) => {
				this.pit = pit;
			});
			return StripHttpPromise(this.$q, promise);
		}

		public CopyPit(pit: IPit): ng.IPromise<IPit> {
			var request: IPitCopy = {
				libraryUrl: this.UserPitLibrary,
				pit: pit
			}
			var promise = this.$http.post(C.Api.Pits, request);
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

		public get IsConfigured(): boolean {
			return onlyIf(this.pit, () => this.latestVersion.configured) || false;
		}

		public get IsSelected(): boolean {
			return !_.isUndefined(this.pit);
		}

		private get latestVersion(): IPitVersion {
			return _.last(this.pit.versions);
		}
	}
}
