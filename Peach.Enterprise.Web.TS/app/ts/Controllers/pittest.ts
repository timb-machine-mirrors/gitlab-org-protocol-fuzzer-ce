/// <reference path="../../../scripts/typings/ng-grid/ng-grid.d.ts" />

module DashApp {
	"use strict";

	import P = Models.Peach;
	import W = Models.Wizard;

	export class PitTestController {
		private pitConfigSvc: Services.IPitConfiguratorService;
		private location: ng.ILocationService;
		private peach: Services.IPeachService;
		private q: ng.IQService;
		private pollerSvc;
		private testPoller;
		private logPoller;
		private POLLER_TIME = 5000;

		public isReadyToTest: boolean = false;

		public get SetVarsComplete(): boolean {
			return this.pitConfigSvc.SetVarsComplete;
		}

		public get FaultDetectionComplete(): boolean {
			return this.pitConfigSvc.FaultMonitorsComplete;
		}

		public get DataCollectionComplete(): boolean {
			return this.pitConfigSvc.DataMonitorsComplete;
		}

		public get AutomationComplete(): boolean {
			return this.pitConfigSvc.AutoMonitorsComplete;
		}

		public testEvents: P.TestEvent[] = [];

		public testStatus: string = "notrunning";

		public log: string = "";

		public tabs: ITab[] = [
			{ title: "Summary", content: "../partials/test-grid.html", active: true, disabled: false },
			{ title: "Log", content: "../partials/test-raw.html", active: false, disabled: false }
		];

		static $inject = ["$scope", "$q", "$location", "poller", "peachService", "pitConfiguratorService"];
		
		constructor($scope: ViewModelScope, $q: ng.IQService, $location: ng.ILocationService, poller, peachService: Services.IPeachService, pitConfiguratorService: Services.IPitConfiguratorService) {
			$scope.vm = this;
			this.pitConfigSvc = pitConfiguratorService;
			this.pollerSvc = poller;
			this.location = $location;
			this.peach = peachService;
			this.q = $q;
			this.testStatus = "notrunning";
		}

		public dataGridOptions: ngGrid.IGridOptions = {
			data: "vm.testEvents",
			columnDefs: [
				{
					field: "status",
					displayName: " ",
					width: 25,
					cellTemplate: "<div class=\"ngCellText\" ng-class=\"col.colIndex()\"><i ng-class=\"{'icon-ok green': row.getProperty(col.field) == 'pass', 'icon-warning-sign orange': row.getProperty(col.field) == 'warn', 'icon-remove red': row.getProperty(col.field) == 'fail'}\" /></div>"
				},
				{
					field: "short",
					displayName: "Message"
				}
			]
		};

		public beginTest() {

			var agents: W.Agent[] = [];
			agents = agents.concat(this.pitConfigSvc.FaultMonitors);

			if (this.pitConfigSvc.DataMonitors !== undefined) {
				agents = agents.concat(this.pitConfigSvc.DataMonitors);
			}

			if (this.pitConfigSvc.AutoMonitors !== undefined)
				agents = agents.concat(this.pitConfigSvc.AutoMonitors);

			var monitorPromise = this.peach.PostMonitors(this.pitConfigSvc.Pit.pitUrl, agents);
			var configPromise = this.peach.PostConfig(this.pitConfigSvc.Pit.pitUrl, this.pitConfigSvc.Defines.config);

			this.q.all([monitorPromise, configPromise]).then((response) => {
				this.peach.TestConfiguration(this.pitConfigSvc.Pit.pitUrl, (data: P.StartTestResponse) => {
					this.startTestPoller(data.testUrl);
					this.startLogPoller(data.testUrl);
				});
			}, (response) => {
				console.error(response);  
			});

		}

		public submitAllInfo() {
			//TODO
			//this.peach.PostConfiguration();
			this.location.path("/configurator/done");
		}

		private startTestPoller(testUrl: string) {
			var testResource = this.peach.GetSingleThing(testUrl);
			this.testPoller = this.pollerSvc.get(testResource, {
				action: "get",
				delay: this.POLLER_TIME,
				method: "GET"
			});

			this.testPoller.promise.then(null, (e) => {
				console.error(e);
			}, (data: P.GetTestUpdateResponse) => {
				this.testEvents = data.events;
				this.testStatus = data.status;
				if (data.status != "active") {
					this.testPoller.stop();
					this.logPoller.stop();
				}
			});
		}

		private startLogPoller(testUrl: string) {
			var url = testUrl + "/raw";
			var logResource = this.peach.GetSingleThing(url);
			this.logPoller = this.pollerSvc.get(logResource, {
				action: "get",
				delay: this.POLLER_TIME,
				method: "GET"
			});

			this.logPoller.promise.then(null, (e) => {
				console.error(e);
			}, (data: string) => {
				this.log = data;
			});
		}
	}
}