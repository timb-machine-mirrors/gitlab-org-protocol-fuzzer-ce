/// <reference path="../reference.ts" />

module Peach {
	"use strict";

	declare function ngGridFlexibleHeightPlugin(opts?: any): void;

	export class ConfigurationTestController {
		private pitConfigSvc: Services.PitConfiguratorService;
		private location: ng.ILocationService;
		private peach: Services.PeachService;
		private q: ng.IQService;
		private pollerSvc;
		private testPoller;
		private POLLER_TIME = 500;

		public isReadyToTest: boolean = false;

		static $inject = ["$scope", "$q", "$location", "poller", "peachService", "pitConfiguratorService"];

		constructor(
			$scope: ViewModelScope,
			$location: ng.ILocationService,
			poller,
			peachService: Services.PeachService,
			pitConfiguratorService: Services.PitConfiguratorService
		) {
			$scope.vm = this;
			this.pitConfigSvc = pitConfiguratorService;
			this.pollerSvc = poller;
			this.location = $location;
			this.peach = peachService;
		}

		public get testEvents(): Models.ITestEvent[] {
			return this.pitConfigSvc.TestEvents;
		}

		public get testStatus(): string {
			return this.pitConfigSvc.TestStatus;
		}

		public get log(): string {
			return this.pitConfigSvc.TestLog;
		}

		public get testTime(): string {
			return this.pitConfigSvc.TestTime;
		}

		public tabs: ITab[] = [
			{ title: "Summary", content: "html/test-grid.html", active: true, disabled: false },
			{ title: "Log", content: "html/test-raw.html", active: false, disabled: false }
		];

		public dataGridOptions: ngGrid.IGridOptions = {
			data: "vm.testEvents",
			columnDefs: [
				{
					field: "status",
					displayName: " ",
					width: 25,
					cellTemplate: "html/test-grid-status-template.html"
				},
				{
					displayName: "Message",
					cellTemplate: "html/test-grid-message-template.html"
				}
			],
			rowHeight: 45,
			plugins: [new ngGridFlexibleHeightPlugin()]
		};

		public beginTest() {
			this.pitConfigSvc.ResetTestData();
			this.pitConfigSvc.Pit.configured = false;
			this.pitConfigSvc.TestTime = moment().format("h:mm a");
			var agents: Models.Agent[] = [];
			agents = agents.concat(this.pitConfigSvc.FaultMonitors);

			if (this.pitConfigSvc.DataMonitors !== undefined) {
				agents = agents.concat(this.pitConfigSvc.DataMonitors);
			}

			if (this.pitConfigSvc.AutoMonitors !== undefined) {
				agents = agents.concat(this.pitConfigSvc.AutoMonitors);
			}
		}

		private startTestPoller(testUrl: string) {
			var testResource = this.peach.GetSingleResource(testUrl);
			this.testPoller = this.pollerSvc.get(testResource, {
				action: "get",
				delay: this.POLLER_TIME,
				method: "GET"
			});

			this.testPoller.promise.then(null, (e) => {
				console.error(e);
			}, (data: Models.ITestResult) => {
				this.pitConfigSvc.TestEvents = data.events;
				this.pitConfigSvc.TestStatus = data.status;
				this.pitConfigSvc.TestLog = data.log;
				if (data.status != "active") {
					this.testPoller.stop();
					//this.logPoller.stop();
					if (data.status == "pass") {
						this.pitConfigSvc.Pit.configured = true;
					}
				}
			});
		}
	}
}
