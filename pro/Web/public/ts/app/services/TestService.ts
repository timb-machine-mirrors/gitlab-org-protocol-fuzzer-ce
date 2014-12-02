/// <reference path="../reference.ts" />

module Peach.Services {
	"use strict";

	export var TEST_INTERVAL = 100;

	export class TestService {

		static $inject = [
			"$http",
			"$interval",
			"PitService"
		];

		constructor(
			private $http: ng.IHttpService,
			private $interval: ng.IIntervalService,
			public pitService: PitService
		) {
			this.reset();
		}

		private testResult: Models.ITestResult;
		public get TestResult(): Models.ITestResult {
			return this.testResult;
		}

		private testTime: string;
		public get TestTime(): string {
			return this.testTime;
		}

		private reset() {
			this.testResult = {
				status: "notrunning",
				log: "",
				events: []
			};
			this.testTime = moment().format("h:mm a");
		}

		public BeginTest() {
			this.reset();

			var promise = this.$http.get('/p/conf/wizard/test/start', {
				params: { pitUrl: this.pitService.Pit.pitUrl }
			});

			promise.success((data: Models.ITestRef) => {
				this.startTestPoller(data.testUrl);
			});
		}

		private startTestPoller(testUrl: string) {
			var interval = this.$interval(() => {
				var promise = this.$http.get(testUrl);
				promise.success((data: Models.ITestResult) => {
					this.testResult = data;
					if (data.status != "active") {
						this.$interval.cancel(interval);
						if (data.status == "pass") {
							this.pitService.IsConfigured = true;
						}
					}
				});
			}, TEST_INTERVAL);
		}
	}
}
