/// <reference path="../includes.d.ts" />

module DashApp {
	"use strict";

	declare function ngGridFlexibleHeightPlugin(opts?: any): void;


	export class ConfigurationVariablesController {

		private peach: Services.IPeachService;
		private pitConfigSvc: Services.IPitConfiguratorService;

		static $inject = ["$scope", "peachService", "pitConfiguratorService"];

		constructor($scope: ViewModelScope, peachService: Services.IPeachService, pitConfiguratorService: Services.IPitConfiguratorService) {
			$scope.vm = this;
			this.peach = peachService;
			this.pitConfigSvc = pitConfiguratorService;
		}

		public definesGridOptions: ngGrid.IGridOptions = {
			data: "vm.DefinesSimple",
			columnDefs: [
				{ field: "name", displayName: "Name", enableCellEdit: false },
				{ field: "key", displayName: "Key", enableCellEdit: false },
				{ field: "value", displayName: "Value", enableCellEdit: true, cellTemplate: "<div class=\"ngCellText\">{{row.getProperty(col.field)}}</div>", editableCellTemplate: "../../partials/definesvalue-edit-template.html" },
			],
			enableCellSelection: false,
			enableRowSelection: false,
			multiSelect: false,
			enableCellEdit: true,
			enableCellEditOnFocus: true,
			plugins: [new ngGridFlexibleHeightPlugin()]
		};

		public get DefinesSimple(): any[] {
			if (this.pitConfigSvc != undefined)
				return this.pitConfigSvc.Defines.config;
			else
				return [];
		}

		public Save(form: ng.IFormController): void {
			this.peach.PostConfig(this.pitConfigSvc.Pit.pitUrl, this.pitConfigSvc.Defines.config).success(() => {
				form.$dirty = false;
			});
		}
	}
}
