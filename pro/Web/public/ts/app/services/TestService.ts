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
			this.Reset();

			this.pendingResult = this.$q.defer<any>();
			this.isPending = true;

			this.testTime = moment().format("h:mm a");

			var request: IJobRequest = {
				pitUrl: this.pitService.Pit.pitUrl,
				isControlIteration: true
			};
			var promise = this.$http.post(C.Api.Jobs, request);
			promise.success((job: IJob) => {
				this.StartTestPoller(job.firstNodeUrl);
			});
			promise.catch(reason => {
				this.SetFailure(reason);
				this.pendingResult.reject();
			});

			return this.pendingResult.promise;
		}

		private Reset() {
			this.testTime = "";
			this.testResult = {
				status: "",
				log: "",
				events: []
			};
		}

		private StartTestPoller(testUrl: string) {
			var interval = this.$interval(() => {
				var promise = this.$http.get(testUrl);
				promise.success((data: ITestResult) => {
					this.testResult = data;
					if (data.status !== TestStatus.Active) {
						this.StopTestPoller(interval);
						var pass = (data.status === TestStatus.Pass);
						if (pass) {
							this.pendingResult.resolve();
						} else {
							this.pendingResult.reject();
						}
					}
				});
				promise.catch((reason: ng.IHttpPromiseCallbackArg<IError>) => {
					this.StopTestPoller(interval);
					this.SetFailure(reason.data.errorMessage);
					this.pendingResult.reject();
				});
			}, TEST_INTERVAL);
		}

		private StopTestPoller(interval: any) {
			this.isPending = false;
			this.$interval.cancel(interval);
		}

		private SetFailure(reason) {
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
