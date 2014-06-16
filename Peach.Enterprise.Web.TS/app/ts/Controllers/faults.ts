/// <reference path="../Models/wizard.ts" />
/// <reference path="../Models/peach.ts" />
/// <reference path="../Models/peach.ts" />
/// <reference path="../../../Scripts/typings/angularjs/angular.d.ts" />

module DashApp {
	export class FaultsController {

		static $inject = ["$scope", "poller", "peachService"];

		constructor($scope, poller, peachService: Services.IPeachService) {

			$scope.faults = [];
			$scope.gridFaults = {
				showGroupPanel: true,
				jqueryUIDraggable: true,
				data: 'faults',
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


		}
	}
}