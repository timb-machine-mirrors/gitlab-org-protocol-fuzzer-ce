/// <reference path="../reference.ts" />

module Peach {
	"use strict";

	export var TEST_INTERVAL = 1000;

	export class TestService {

		static $inject = [
			C.Angular.$rootScope,
			C.Angular.$q,
			C.Angular.$http,
			C.Angular.$interval,
			C.Services.Pit
		];

		constructor(
			private $rootScope: ng.IRootScopeService,
			private $q: ng.IQService,
			private $http: ng.IHttpService,
			private $interval: ng.IIntervalService,
			private pitService: PitService
		) {
			$rootScope.$on(C.Events.PitChanged,() => {
				this.reset();
			});
		}

		private pendingResult: ng.IDeferred<any>;
		private isPending: boolean = false;
		private testResult: ITestResult;
		private testTime: string;

		public get IsPending(): boolean {
			return this.isPending;
		}

		public get TestResult(): ITestResult {
			return this.testResult;
		}

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

			this.testTime = moment().format("h:mm a");

			var request: IJobRequest = {
				pitUrl: this.pitService.Pit.pitUrl,
				isControlIteration: true
			};

			this.$http.post(C.Api.Jobs, request)
				.success((job: IJob) => {
					this.startTestPoller(job.firstNodeUrl);
				})
				.catch(reason => {
					this.setFailure(reason);
					this.pendingResult.reject();
				})
			;

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
				this.$http.get(testUrl)
					.success((data: ITestResult) => {
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
					})
					.catch((reason: ng.IHttpPromiseCallbackArg<IError>) => {
						this.stopTestPoller(interval);
						this.setFailure(reason.data.errorMessage);
						this.pendingResult.reject();
					})
				;
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
