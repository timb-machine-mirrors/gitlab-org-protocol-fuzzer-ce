/// <reference path="../../../scripts/typings/ng-grid/ng-grid.d.ts" />

module DashApp {
	"use strict";

	import P = Models.Peach;

	export class PitTestController {
		private storage: ng.ILocalStorageService;
		private location: ng.ILocationService;
		private peach: Services.IPeachService;

		public isReadyToTest: boolean = false;

		constructor($scope: ViewModelScope, $routeParams: IWizardParams, $location: ng.ILocationService, peachService: Services.IPeachService, localStorageService: ng.ILocalStorageService) {
			$scope.vm = this;
			this.storage = localStorageService;
			this.location = $location;
			this.peach = peachService;
		}
		//"<div><i ng-class=\"{icon-ok-sign: row.getProperty(col.field) == 'Success'}\" /></div>"
		//rowTemplate: "/partials/test-result.html"
		public dataGridOptions: ngGrid.IGridOptions = {
			data: "vm.testResults",
			columnDefs: [
				{
					field: "status",
					displayName: " ",
					width: 25,
					cellTemplate: "<div class=\"ngCellText\" ng-class=\"col.colIndex()\"><i ng-class=\"{'icon-ok green': row.getProperty(col.field) == 'Success', 'icon-warning-sign orange': row.getProperty(col.field) == 'Warning', 'icon-remove red': row.getProperty(col.field) == 'Failure'}\" /></div>"
				},
				{
					field: "message",
					displayName: "Message"
				}
			]
		};

		public testResults: P.PeachTestResult[];

		public beginTest() {
			var coldef: ngGrid.IColumnDef;
			this.dataGridOptions.rowTemplate;
			this.peach.TestConfiguration().query({}, (data: P.PeachTestResult[]) => {
				this.testResults = data;
			});
		}

		public submitAllInfo() {
			//TODO
			//this.peach.PostConfiguration();
			this.location.path("/configurator/done");
		}
	}
}