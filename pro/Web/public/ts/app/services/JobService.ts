﻿ /// <reference path="../reference.ts" />

module Peach {
	"use strict";

	export var JOB_INTERVAL = 500;

	export class JobService {
		static $inject = [
			"$q",
			"$http",
			"$interval",
			"PitService"
		];

		constructor(
			private $q: ng.IQService,
			private $http: ng.IHttpService,
			private $interval: ng.IIntervalService,
			public pitService: PitService
		) {
		}

		private poller: ng.IPromise<any>;

		private job: IJob;
		public get Job(): IJob {
			return this.job;
		}

		private faults: IFaultSummary[] = [];
		public get Faults(): IFaultSummary[] {
			return this.faults;
		}

		public get CanStart(): boolean {
			return onlyIf(this.pitService.IsSelected, () => 
				_.isUndefined(this.job) || this.job.status === JobStatus.Stopped
			);
		}

		public get CanContinue(): boolean {
			return this.checkStatus([JobStatus.Paused]);
		}

		public get CanPause(): boolean {
			return this.checkStatus([JobStatus.Running]);
		}

		public get CanStop(): boolean {
			return this.checkStatus([
				JobStatus.Running,
				JobStatus.Paused,
				JobStatus.StartPending,
				JobStatus.PausePending,
				JobStatus.ContinuePending
			]);
		}

		public get RunningTime(): string {
			if (this.job === undefined) {
				return "";
			}
			return moment(new Date(0, 0, 0, 0, 0, this.job.runtime)).format("H:mm:ss");
		}

		public GetJobs(): ng.IPromise<void> {
			var deferred = this.$q.defer<void>();
			var promise = this.$http.get("/p/jobs");
			promise.success((jobs: IJob[]) => {
				var hasPit = false;
				if (jobs.length > 0) {
					this.job = _.first(jobs);
					hasPit = !_.isEmpty(this.job.pitUrl);
					if (hasPit) {
						var promise2 = this.pitService.SelectPit(this.job.pitUrl);
						promise2.then(() => {
							deferred.resolve();
						});
					}
					this.startJobPoller();
				}
				if (!hasPit) {
					deferred.resolve();
				}
			});
			promise.error(reason => this.onError(reason));
			return deferred.promise;
		}

		public StartJob(job?: IJob) {
			if (job === undefined) {
				job = { pitUrl: this.pitService.Pit.pitUrl };
			} else {
				job.pitUrl = this.pitService.Pit.pitUrl;
			}

			if (this.CanStart) {
				var promise = this.$http.post("/p/jobs", job);
				promise.success((newJob: IJob) => {
					this.job = newJob;
					this.startJobPoller();
				});
				promise.error(reason => this.onError(reason));
			} else if (this.CanContinue) {
				this.job.status = JobStatus.ActionPending;
				this.$http.get(this.job.jobUrl + "/continue")
					.success(() => this.startJobPoller())
					.error(reason => this.onError(reason));
			}
		}

		public PauseJob() {
			if (this.CanPause) {
				this.job.status = JobStatus.ActionPending;
				this.$http.get(this.job.jobUrl + "/pause").error(reason => this.onError(reason));
			}
		}

		public StopJob() {
			if (this.CanStop) {
				this.job.status = JobStatus.ActionPending;
				this.$http.get(this.job.jobUrl + "/stop").error(reason => this.onError(reason));
			}
		}

		private checkStatus(good: string[]): boolean {
			return (
				this.job &&
				_.contains(good, this.job.status) &&
				this.pitService.IsSelected	
			);
		}

		private onError(response) {
			alert("Peach is busy with another task.\n" +
				"Confirm that there aren't multiple browsers accessing the same instance of Peach.");
			this.job = undefined;
			this.$interval.cancel(this.poller);
		}

		private startJobPoller() {
			if (!_.isUndefined(this.poller)) {
				return;
			}

			this.poller = this.$interval(() => {
				var promise = this.$http.get(this.job.jobUrl);
				promise.success((job: IJob) => {
					this.job = job;
					if (job.status === JobStatus.Stopped ||
						job.status === JobStatus.Paused) {
						this.$interval.cancel(this.poller);
						this.poller = undefined;
					}
					if (this.faults.length !== job.faultCount) {
						this.reloadFaults();
					}
				});
				promise.error(reason => this.onError(reason));
			}, JOB_INTERVAL);
		}

		private reloadFaults() {
			var promise = this.$http.get(this.job.faultsUrl);
			promise.success((faults: IFaultSummary[]) => {
				this.faults = faults;
			});
			promise.error(reason => this.onError(reason));
		}
	}
}
