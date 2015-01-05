/// <reference path="../reference.ts" />

module Peach {
	"use strict";

	export var TEST_INTERVAL = 1000;

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
			pitService.OnPitChanged(() => this.reset());
		}

		private isPending: boolean = false;
		public get IsPending(): boolean {
			return this.isPending;
		}

		private testResult: ITestResult;
		public get TestResult(): ITestResult {
			return this.testResult;
		}

		private testTime: string;
		public get TestTime(): string {
			return this.testTime;
		}

		public get CanBeginTest(): boolean {
			return !this.isPending;
		}

		public BeginTest() {
			this.reset();

			this.isPending = true;
			var promise = this.$http.get('/p/conf/wizard/test/start', {
				params: { pitUrl: this.pitService.Pit.pitUrl }
			});

			promise.success((data: ITestRef) => {
				this.startTestPoller(data.testUrl);
			});
			promise.catch(reason => {
				this.setFailure(reason);
			});
		}

		private reset() {
			this.testResult = {
				status: "",
				log: "",
				events: []
			};
			this.testTime = moment().format("h:mm a");
		}

		private startTestPoller(testUrl: string) {
			var interval = this.$interval(() => {
				var promise = this.$http.get(testUrl);
				promise.success((data: ITestResult) => {
					this.testResult = data;
					if (data.status !== TestStatus.Active) {
						this.stopTestPoller(interval);
					}
				});
				promise.catch(reason => {
					this.stopTestPoller(interval);
					this.setFailure(reason);
				});
			}, TEST_INTERVAL);
		}

		private stopTestPoller(interval: any) {
			this.isPending = false;
			this.$interval.cancel(interval);
			this.pitService.ReloadPit();
		}

		private setFailure(response: ng.IHttpPromiseCallbackArg<IError>) {
			this.isPending = false;
			this.testResult.status = TestStatus.Fail;

			var event: ITestEvent = {
				id: this.testResult.events.length + 1,
				status: TestStatus.Fail,
				short: '',
				description: 'Test execution failure.',
				resolve: response.data.errorMessage
			};

			this.testResult.events.push(event);
		}
	}
}
