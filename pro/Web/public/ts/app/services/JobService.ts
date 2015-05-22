﻿ /// <reference path="../reference.ts" />

module Peach {
	"use strict";

	export var JOB_INTERVAL = 1000;
	
	export class JobService {
		static $inject = [
			C.Angular.$q,
			C.Angular.$http,
			C.Angular.$state,
			C.Angular.$interval,
			C.Services.Pit
		];

		constructor(
			private $q: ng.IQService,
			private $http: ng.IHttpService,
			private $state: ng.ui.IStateService,
			private $interval: ng.IIntervalService
		) {
		}

		private jobs: IJob[] = [];

		private poller: ng.IPromise<any>;
		private job: IJob;
		private faults: IFaultSummary[] = [];
		
		public OnEnter(id) {
			this.$http.get(C.Api.JobUrl.replace(':id', id))
				.success((job: IJob) => {
					this.job = job;
				});
		}
		
		public OnExit() {
			this.job = undefined;
			this.faults = [];
		}
		
		public get Jobs(): IJob[]{
			return this.jobs;
		}
		
		public get Job(): IJob {
			return this.job;
		}

		public get Faults(): IFaultSummary[] {
			return this.faults;
		}

		public get IsRunning(): boolean {
			return this.Job && this.Job.status === JobStatus.Running;
		}

		public get IsPaused(): boolean {
			return this.Job && this.Job.status === JobStatus.Paused;
		}

		public get CanStart(): boolean {
			return _.isUndefined(this.Job) || this.Job.status === JobStatus.Stopped;
		}

		public get CanContinue(): boolean {
			return this.CheckStatus([JobStatus.Paused]);
		}

		public get CanPause(): boolean {
			return this.CheckStatus([JobStatus.Running]);
		}

		public get CanStop(): boolean {
			return this.CheckStatus([
				JobStatus.Running,
				JobStatus.Paused,
				JobStatus.StartPending,
				JobStatus.PausePending,
				JobStatus.ContinuePending
			]);
		}

		private CheckStatus(good: string[]): boolean {
			return this.Job && _.contains(good, this.Job.status);
		}

		public get RunningTime(): string {
			if (_.isUndefined(this.Job)) {
				return undefined;
			}
			return moment(new Date(0, 0, 0, 0, 0, this.Job.runtime)).format("H:mm:ss");
		}

		public LoadFaultDetail(id: string): ng.IPromise<IFaultDetail> {
			var fault = _.find(this.faults, { iteration: id });
			if (_.isUndefined(fault)) {
				var defer = this.$q.defer<IFaultDetail>();
				defer.reject();
				return defer.promise;
			}
			return StripHttpPromise(this.$q, this.$http.get(fault.faultUrl));
		}

		public GetJobs(): ng.IPromise<IJob[]> {
			var params = { filter: 'dryRun' };
			var promise = this.$http.get(C.Api.Jobs, { params: params });
			return StripHttpPromise(this.$q, promise);
		}

		public Start(job: IJobRequest): ng.IPromise<IJob> {
			if (this.CanStart) {
				var promise = this.$http.post(C.Api.Jobs, job);
				promise.success((job: IJob) => {
					this.StartJobPoller(job);
				});
				promise.error(reason => {
					console.log('JobService.StartJob().error>', reason);
				});
				return StripHttpPromise(this.$q, promise);
			}
		}
		
		public Continue() {
			if (this.CanContinue) {
				this.Job.status = JobStatus.ContinuePending;
				this.$http.get(this.Job.commands.continueUrl)
					.success(() => this.StartJobPoller(this.JobEntry))
					.error(reason => this.OnError(this.JobEntry, reason));
			}
		}

		public Pause() {
			if (this.CanPause) {
				this.Job.status = JobStatus.PausePending;
				this.$http.get(this.Job.commands.pauseUrl)
					.success(() => this.StartJobPoller(this.JobEntry))
					.error(reason => this.OnError(this.JobEntry, reason));
			}
		}

		public Stop() {
			if (this.CanStop) {
				this.Job.status = JobStatus.StopPending;
				this.$http.get(this.Job.commands.stopUrl)
					.success(() => this.StartJobPoller(this.JobEntry))
					.error(reason => this.OnError(this.JobEntry, reason));
			}
		}

		private OnError(entry: IJob, error: any) {
			console.log('onError', error);
			this.StopJobPoller(job);
		}

		private StartJobPoller(job: IJob) {
			if (!_.isUndefined(this.poller)) {
				return;
			}
			
			console.log('StartJobPoller:', entry.id);

			entry.poller = this.$interval(() => {
				var promise = this.$http.get(entry.job.jobUrl);
				promise.success((job: IJob) => {
					console.log("job:", job);
					entry.job = job;
					if (job.status === JobStatus.Stopped ||
						job.status === JobStatus.Paused) {
						this.StopJobPoller(entry);
					}
					if (this.faults.length !== job.faultCount) {
						this.ReloadFaults(job);
					}
				});
				promise.error(reason => this.OnError(entry, reason));
			}, JOB_INTERVAL);
		}
		
		private StopJobPoller() {
			console.log("StopJobPoller:");

			this.$interval.cancel(this.poller);
			this.poller = undefined;
		}

		private ReloadFaults(job: IJob) {
			console.log("ReloadFaults:", job);
			var promise = this.$http.get(job.faultsUrl);
			promise.success((faults: IFaultSummary[]) => {
				this.faults = faults;
			});
//			promise.error(reason => this.OnError(reason));
		}
	}
}
