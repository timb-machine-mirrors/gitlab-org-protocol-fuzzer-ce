/// <reference path="../reference.ts" />

module Peach.Services {
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
			private PitResource: Models.IPitResource,
			private PitLibraryResource: Models.ILibraryResource,
			private PitConfigResource: Models.IPitConfigResource,
			private PitAgentsResource: Models.IPitAgentsResource
		) {
			PitLibraryResource.query((libs: Models.ILibrary[]) => {
				var userLibs = libs.filter((item: Models.ILibrary) => item.locked === false);
				this.userPitLibrary = _.first(userLibs).libraryUrl;
			});
		}

		private userPitLibrary: string;
		public get UserPitLibrary(): string {
			return this.userPitLibrary;
		}

		private pit: Models.IPit;
		public get Pit(): Models.IPit {
			return this.pit;
		}

		private pitConfig: Models.IPitConfig;
		public get PitConfig(): Models.IPitConfig {
			return this.pitConfig;
		}

		private pitAgents: Models.IPitAgents;
		public get PitAgents(): Models.IPitAgents {
			return this.pitAgents;
		}

		private testResult: Models.ITestResult;
		public get TestResult(): Models.ITestResult {
			return this.testResult;
		}

		// MainController 
		// -> PitLibraryController (modal)
		//    -> SelectPit 
		//       -> CopyPitController (modal)
		//          -> CopyPit
		public SelectPit(url: string): ng.IPromise<Models.IPit> {
			var deferred = this.$q.defer<Models.IPit>();
			var promise = this.PitResource.get({ id: ExtractId('pits', url) }).$promise;
			promise.then((pit: Models.IPit) => {
				if (pit.locked) {
					var modal = this.$modal.open({
						templateUrl: "html/modal/CopyPit.html",
						controller: CopyPitController,
						resolve: { pit: () => pit }
					});
					modal.result.then((copied: Models.IPit) => {
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

		public CopyPit(pit: Models.IPit): ng.IPromise<Models.IPit> {
			var request: Models.IPitCopy = {
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

		public LoadPitConfig(): Models.IPitConfig {
			return onlyIf(this.Pit, () => {
				return this.PitConfigResource.get({ id: this.PitId }, (data: Models.IPitConfig) => {
					this.pitConfig = data;
				});
			});
		}

		public LoadPitAgents(): Models.IPitAgents {
			return onlyIf(this.Pit, () => {
				return this.PitAgentsResource.get({ id: this.PitId }, (data: Models.IPitAgents) => {
					this.pitAgents = data;
				});
			});
		}

		public SavePitAgents(agents: Models.Agent[]) {
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

		private get latestVersion(): Models.IPitVersion {
			return _.last(this.Pit.versions);
		}
	}
}
