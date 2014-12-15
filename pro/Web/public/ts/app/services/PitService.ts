/// <reference path="../reference.ts" />

module Peach {
	"use strict";

	export class PitService {

		static $inject = [
			"$q",
			"$modal",
			"PitResource",
			"PitLibraryResource",
			"PitConfigResource",
			"PitAgentsResource"
		];

		constructor(
			private $q: ng.IQService,
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
			var deferred = this.$q.defer<IPit>();
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
						this.pit = copied;
						deferred.resolve(this.Pit);
					});
					modal.result.catch((reason) => {
						deferred.reject(reason);
					});
				} else {
					this.pit = pit;
					deferred.resolve(this.Pit);
				}
			});
			promise.catch((reason) => {
				deferred.reject(reason);
			});
			return deferred.promise;
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
			return onlyIf(this.Pit, () => this.Pit.name) || '(none)';
		}

		public get PitId(): string {
			return onlyIf(this.Pit, () => ExtractId('pits', this.pit.pitUrl));
		}

		public LoadPitConfig(): IPitConfig {
			return onlyIf(this.Pit, () => {
				return this.PitConfigResource.get({ id: this.PitId }, (data: IPitConfig) => {
					this.pitConfig = data;
				});
			});
		}

		public LoadPitAgents(): IPitAgents {
			return onlyIf(this.Pit, () => {
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
			return onlyIf(this.Pit, () => this.latestVersion.configured) || false;
		}

		private get latestVersion(): IPitVersion {
			return _.last(this.Pit.versions);
		}
	}
}
