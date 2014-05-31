/// <reference path="../Models/wizard.ts" />
/// <reference path="../Models/peach.ts" />
/// <reference path="../../../Scripts/typings/angularjs/angular.d.ts" />
/// <reference path="../../../scripts/typings/angular-ui-bootstrap/angular-ui-bootstrap.d.ts" />

module DashApp {

	import P = Models.Peach;

	export class MainController {
		private storage: ng.ILocalStorageService;
		private peachService: Services.IPeachService;
		private modal: ng.ui.bootstrap.IModalService;

		//#region Public Properties
		private _pit: P.Pit;
		public get pit(): P.Pit {
			if (this._pit != undefined)
				return this._pit;
			else
				return this.storage.get(StorageStrings.Pit);
		}

		public set pit(pit: P.Pit) {
			if (this._pit != pit) {
				this._pit = pit;
				this.storage.set(StorageStrings.Pit, pit);
			}
		}

		public location: ng.ILocationService;
		public job: P.Job;
		//#endregion


		static $inject = ["$scope", "$resource", "$location", "$modal", "poller", "peachService", "localStorageService"];

		constructor($scope: ViewModelScope, $resource, $location: ng.ILocationService, $modal: ng.ui.bootstrap.IModalService, poller, peachService: Services.IPeachService, localStorageService: ng.ILocalStorageService) {
			$scope.vm = this;

			this.modal = $modal;
			this.peachService = peachService;
			this.location = $location;
			this.storage = localStorageService;
			this.storage.remove("pit");

			var jobResource = peachService.GetJob();

			var jobPoller = poller.get(jobResource);

			jobPoller.promise.then(null, null, function (data) {
				this.job = data;
			});

			this.showPitSelector();
		}

		public showPitSelector() {
			this.modal.open({
				templateUrl: "/partials/pitlibrary.html",
				keyboard: false,
				backdrop: 'static',
				controller: PitLibraryController,
				resolve: {
					peachsvc: () => {
						return this.peachService;
					}
				}
			}).result.then((pitUrl: string) => {
				this.peachService.GetPit(pitUrl, (data: P.Pit) => { this.pit = data });
				});
		}
	}

	export interface ViewModelScope extends ng.IScope {
		vm: any;
	}

	export class StorageStrings {
		static Pit = "pit";
		static FaultMonitors = "faultMonitors";
		static DataMonitors = "dataMonitors";
		static AutoMonitors = "autoMonitors";
		static PitDefines = "pitDefines";
	}
}