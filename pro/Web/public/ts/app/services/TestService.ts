/// <reference path="../reference.ts" />

module Peach {
	"use strict";

	export var TEST_INTERVAL = 1000;

	export class TestService {

		static $inject = [
			C.Angular.$q,
			C.Angular.$http,
			C.Angular.$interval,
			C.Services.Pit
		];

		constructor(
			private $q: ng.IQService,
			private $http: ng.IHttpService,
			private $interval: ng.IIntervalService,
			private pitService: PitService
		) {
			this.reset();
			pitService.OnPitChanged(() => this.reset());
		}

		private pendingResult: ng.IDeferred<any>;
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

		public get IsAvailable(): boolean {
			return !_.isEmpty(this.testTime);
		}

		public BeginTest(): ng.IPromise<any> {
			this.reset();

			this.pendingResult = this.$q.defer<any>();
			this.isPending = true;

			var promise = this.$http.get(C.Api.TestStart, {
				params: { pitUrl: this.pitService.Pit.pitUrl }
			});

			this.testTime = moment().format("h:mm a");

			promise.success((data: ITestRef) => {
				this.startTestPoller(data.testUrl);
			});
			promise.catch(reason => {
				this.setFailure(reason);
				this.pendingResult.reject();
			});

			return this.pendingResult.promise;
		}

		private reset() {
			this.testTime = "";
			this.testResult = {
				status: "",
				log: "",
				events: []
			};
		}

		private startTestPoller(testUrl: string) {
			var interval = this.$interval(() => {
				var promise = this.$http.get(testUrl);
				promise.success((data: ITestResult) => {
					this.testResult = data;
					if (data.status !== TestStatus.Active) {
						this.stopTestPoller(interval);
						var pass = (data.status === TestStatus.Pass);
						if (pass) {
							this.pendingResult.resolve();
						} else {
							this.pendingResult.reject();
						}
					}
				});
				promise.catch((reason: ng.IHttpPromiseCallbackArg<IError>) => {
					this.stopTestPoller(interval);
					this.setFailure(reason.data.errorMessage);
					this.pendingResult.reject();
				});
			}, TEST_INTERVAL);
		}

		private stopTestPoller(interval: any) {
			this.isPending = false;
			this.$interval.cancel(interval);
		}

		private setFailure(reason) {
			this.testResult.status = TestStatus.Fail;

			var event: ITestEvent = {
				id: this.testResult.events.length + 1,
				status: TestStatus.Fail,
				description: 'Test execution failure.',
				resolve: reason
			};

			this.testResult.events.push(event);
		}
	}
}
