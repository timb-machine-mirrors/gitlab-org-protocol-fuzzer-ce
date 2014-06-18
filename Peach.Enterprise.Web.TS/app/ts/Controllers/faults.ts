/// <reference path="../Models/wizard.ts" />
/// <reference path="../Models/peach.ts" />
/// <reference path="../Models/peach.ts" />
/// <reference path="../../../Scripts/typings/angularjs/angular.d.ts" />

module DashApp {
	export class FaultsController {
		private pitConfigSvc: Services.IPitConfiguratorService;

		public get faults() {
			return this.pitConfigSvc.Faults;
		}

		public gridFaults = {
			showGroupPanel: true,
			jqueryUIDraggable: true,
			data: 'vm.faults',
			sortInfo: { fields: ['iteration'], directions: ['desc'] },
			columnDefs: [
				{ field: 'iteration', displayName: '#' },
				{ field: 'timestamp', displayName: 'When' },
				{ field: 'source', displayName: 'Monitor' },
				{ field: 'exploitability', displayName: 'Risk' },
				{ field: 'majorHash', displayName: 'Major Hash' },
				{ field: 'minorHash', displayName: 'Minor Hash' }
			]
		}

		static $inject = ["$scope", "poller", "pitConfiguratorService"];

		constructor($scope: ViewModelScope, poller, pitConfiguratorService: Services.IPitConfiguratorService) {
			$scope.vm = this;
			this.pitConfigSvc = pitConfiguratorService;
		}
	}
}