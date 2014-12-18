/// <reference path="../reference.ts" />

module Peach {
	"use strict";

	export class PitService {

		static $inject = [
			"$q",
			"$window",
			"$modal",
			"PitResource",
			"PitLibraryResource",
			"PitConfigResource",
			"PitAgentsResource"
		];

		constructor(
			private $q: ng.IQService,
			private $window: ng.IWindowService,
			private $modal: ng.ui.bootstrap.IModalService,
			private PitResource: IPitResource,
			private PitLibraryResource: ILibraryResource,
			private PitConfigResource: IPitConfigResource,
			private PitAgentsResource: IPitAgentsResource
		) {
			PitLibraryResource.query((libs: ILibrary[]) => {
				var userLibs = libs.filter((item: ILibrary) => item.locked === false);
				this.userPitLibrary = _.first(userLibs).libraryUrl;
			});
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

		private pitConfig: IPitConfig;
		public get PitConfig(): IPitConfig {
			return this.pitConfig;
		}

		private pitAgents: IPitAgents;
		public get PitAgents(): IPitAgents {
			return this.pitAgents;
		}

		// MainController 
		// -> PitLibraryController (modal)
		//    -> SelectPit 
		//       -> CopyPitController (modal)
		//          -> CopyPit
		public SelectPit(url: string): ng.IPromise<IPit> {
			this.pendingPit = this.$q.defer<IPit>();
			var promise = this.PitResource.get({ id: ExtractId('pits', url) }).$promise;
			promise.then((pit: IPit) => {
				if (pit.locked) {
					var modal = this.$modal.open({
						templateUrl: "html/modal/CopyPit.html",
						controller: CopyPitController,
						resolve: { pit: () => pit }
					});
					modal.result.then((copied: IPit) => {
						// only update the current Pit if successful
						// a failed copy leaves the current Pit untouched
						this.setPit(copied);
						this.pendingPit.resolve(this.pit);
					});
					modal.result.catch((reason) => {
						this.pendingPit.reject(reason);
					});
				} else {
					this.setPit(pit);
					this.pendingPit.resolve(this.pit);
				}
			});
			promise.catch((reason) => {
				this.pendingPit.reject(reason);
			});
			return this.pendingPit.promise;
		}

		private setPit(pit: IPit) {
			this.pit = pit;
			this.$window.sessionStorage.setItem('pitId', this.PitId);
		}

		public ReloadPit() {
			this.pit.$get({ id: this.PitId });
		}

		public CopyPit(pit: IPit): ng.IPromise<IPit> {
			var request: IPitCopy = {
				libraryUrl: this.UserPitLibrary,
				pit: pit
			}
			var result = new this.PitResource(request);
			return result.$save();
		}

		public get Name(): string {
			return onlyIf(this.pit, () => this.pit.name) || '(none)';
		}

		public get PitId(): string {
			return onlyIf(this.pit, () => ExtractId('pits', this.pit.pitUrl));
		}

		public LoadPitConfig(): ng.IPromise<IPitConfig> {
			if (this.pendingPit) {
				var deferred = this.$q.defer<IPitConfig>();
				this.pendingPit.promise.then(() => {
					return this._loadPitConfig().$promise;
				}).then((pitConfig: IPitConfig) => {
					deferred.resolve(pitConfig);
				}).catch(reason => {
					deferred.reject(reason);
				});
				return deferred.promise;
			}
			if (!this.pit) {
				var none = this.$q.defer<IPitConfig>();
				none.reject('No pit selected');
				return none.promise;
			}
			return this._loadPitConfig().$promise;
		}

		private _loadPitConfig(): IPitConfig {
			return this.PitConfigResource.get({ id: this.PitId }, (data: IPitConfig) => {
				this.pitConfig = data;
			});
		}

		public LoadPitAgents(): IPitAgents {
			return onlyIf(this.pit, () => {
				return this.PitAgentsResource.get({ id: this.PitId }, (data: IPitAgents) => {
					this.pitAgents = data;
				});
			});
		}

		public SavePitAgents(agents: Agent[]) {
			if (!this.pitAgents) {
				this.pitAgents = new this.PitAgentsResource();
			}
			this.pitAgents.agents = agents;
			this.pitAgents.pitUrl = this.pit.pitUrl;
			this.pitAgents.$save({ id: this.PitId });
		}

		public get IsConfigured(): boolean {
			return onlyIf(this.pit, () => this.latestVersion.configured) || false;
		}

		private get latestVersion(): IPitVersion {
			return _.last(this.pit.versions);
		}
	}
}
