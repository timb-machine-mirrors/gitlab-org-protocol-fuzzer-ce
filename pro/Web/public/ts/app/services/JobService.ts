 /// <reference path="../reference.ts" />

module Peach {
	"use strict";

	export var JOB_INTERVAL = 1000;
	
	interface IJobEntry {
		id: string;
		job: IJob;
		poller: ng.IPromise<any>;
		error: any;
	}
	
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

		private jobs: IJobEntry[] = [];
		private faults: IFaultSummary[] = [];
		
		public OnEnter(id) {
			this.GetJobs().then(() => {
				var entry = this.GetJobEntry(id);
				console.log('OnEnter.then', id, this.jobs, entry);
				this.ReloadFaults(entry.job);
			});
		}
		
		public OnExit() {
			console.log('OnExit');
			this.faults = [];
		}
		
		public get CurrentJobId(): string {
			return this.$state.params['job'];
		}

		private GetJobEntry(id): IJobEntry {
			return _.find(this.jobs, { id: id });
		}
		
		public GetJob(id): IJob {
			var entry = this.GetJobEntry(id);
			return entry ? entry.job : undefined;
		}

		public get Jobs(): IJob[] {
			return _.pluck(this.jobs, 'job');
		}
		
		public get JobEntry(): IJobEntry {
			return this.GetJobEntry(this.CurrentJobId);
		}

		public get Job(): IJob {
			return this.GetJob(this.CurrentJobId);
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
			var promise = this.$http.get(C.Api.Jobs);
			promise.success((jobs: IJob[]) => {
				// ignore test jobs for now
				this.jobs = _(jobs)
					.filter({ isControlIteration: false })
					.map(job => {
						var entry = this.GetJobEntry(job.id);
						console.log('GetJobs.success, entry:', entry);
						var result = <IJobEntry> {
							id: job.id,
							job: job,
							poller: entry ? entry.poller : undefined
						};
						if (job.status !== JobStatus.Stopped) {
							this.StartJobPoller(result);
						}
						return result;
					})
					.value();
			});
			return StripHttpPromise(this.$q, promise);
		}

		public Start(job: IJobRequest): ng.IPromise<IJob> {
			if (this.CanStart) {
				var promise = this.$http.post(C.Api.Jobs, job);
				promise.success((job: IJob) => {
					console.log('Start:', job.id);
					var entry = <IJobEntry> {
						id: job.id,
						job: job
					};
					this.jobs.push(entry);
					this.StartJobPoller(entry);
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

		private OnError(entry: IJobEntry, error: any) {
			console.log('onError', error);
			this.StopJobPoller(entry);
		}

		private StartJobPoller(entry: IJobEntry) {
			if (!_.isUndefined(entry.poller)) {
				return;
			}
			
			console.log('StartJobPoller:', entry.id);

			entry.poller = this.$interval(() => {
				var promise = this.$http.get(entry.job.jobUrl);
				promise.success((job: IJob) => {
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
		
		private StopJobPoller(entry: IJobEntry) {
			this.$interval.cancel(entry.poller);
			entry.poller = undefined;
		}

		private ReloadFaults(job: IJob) {
			var promise = this.$http.get(job.faultsUrl);
			promise.success((faults: IFaultSummary[]) => {
				this.faults = faults;
			});
//			promise.error(reason => this.OnError(reason));
		}
	}
}
