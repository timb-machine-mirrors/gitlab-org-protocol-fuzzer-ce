 /// <reference path="../reference.ts" />

namespace Peach {
	"use strict";

	export var JOB_INTERVAL = 3000;
	
	export class JobService {
		static $inject = [
			C.Angular.$rootScope,
			C.Angular.$q,
			C.Angular.$http,
			C.Angular.$modal,
			C.Angular.$state,
			C.Angular.$timeout,
			C.Services.Pit
		];

		constructor(
			private $rootScope: ng.IRootScopeService,
			private $q: ng.IQService,
			private $http: ng.IHttpService,
			private $modal: ng.ui.bootstrap.IModalService,
			private $state: ng.ui.IStateService,
			private $timeout: ng.ITimeoutService
		) {
		}

		private jobs: IJob[] = [];
		private poller: ng.IPromise<any>;
		private job: IJob;
		private faults: IFaultSummary[] = [];
		private pending: ng.IPromise<IJob>;
		private isActive: boolean;
		
		public OnEnter(id: string): void {
			this.isActive = true;
			this.onPoll(C.Api.JobUrl.replace(':id', id));
		}
		
		public OnExit(): void {
			this.isActive = false;

			if (this.poller) {
				this.$timeout.cancel(this.poller);
				this.poller = undefined;
			}

			this.job = undefined;
			this.$rootScope['job'] = undefined;
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

		public get CanContinue(): boolean {
			return this.isControlable && this.Job.status === JobStatus.Paused;
		}

		public get CanPause(): boolean {
			return this.isControlable && this.Job.status === JobStatus.Running;
		}

		public get CanStop(): boolean {
			return this.isControlable && (
				this.Job.status === JobStatus.Starting ||
				this.Job.status === JobStatus.Running ||
				this.Job.status === JobStatus.Paused
			);
		}

		public get CanKill(): boolean {
			return this.isControlable && this.Job.status === JobStatus.Stopping;
		}

		private get isControlable(): boolean {
			return this.Job && !_.isUndefined(this.Job.commands);
		}

		public get RunningTime(): string {
			if (_.isUndefined(this.Job)) {
				return undefined;
			}
			var duration = moment.duration(this.job.runtime, 'seconds');
			if (duration.asDays() >= 1) {
				return '{0}d {1}h {2}m'.format(
					Math.floor(duration.asDays()),
					duration.hours().toString().paddingLeft('00'),
					duration.minutes().toString().paddingLeft('00')
				);
			} else {
				return '{0}h {1}m {2}s'.format(
					duration.hours().toString().paddingLeft('00'),
					duration.minutes().toString().paddingLeft('00'),
					duration.seconds().toString().paddingLeft('00')
				);
			}
		}

		private doLoadFaultDetail(defer: ng.IDeferred<IFaultDetail>, id: string) {
			var fault = _.find(this.faults, { iteration: id });
			if (_.isUndefined(fault)) {
				defer.reject();
			} else {
				this.$http.get(fault.faultUrl)
					.success((data: IFaultDetail) => { defer.resolve(data); })
					.error(reason => { defer.reject(reason); })
				;
			}
		}

		public LoadFaultDetail(id: string): ng.IPromise<IFaultDetail> {
			var defer = this.$q.defer<IFaultDetail>();
			if (this.pending) {
				this.pending.finally(() => { this.doLoadFaultDetail(defer, id); });
			} else {
				this.doLoadFaultDetail(defer, id);
			}
			return defer.promise;
		}

		public GetJobs(): ng.IPromise<IJob[]> {
			var params = { dryrun: false };
			var promise = this.$http.get(C.Api.Jobs, { params: params })
				.success((jobs: IJob[]) => this.jobs = jobs);
			return StripHttpPromise(this.$q, promise);
		}

		public Start(job: IJobRequest): ng.IPromise<IJob> {
			var promise = this.$http.post(C.Api.Jobs, job);
			promise.catch((reason: ng.IHttpPromiseCallbackArg<IError>) => {
				var options: IAlertOptions = {
					Title: 'Error Starting Job',
					Body: 'Peach was unable to start a new job.',
				};

				// 404 = Pit not found
				// 403 = Job already running

				if (reason.status === 403) {
					options.Body += "\n\nPlease ensure another job is not running and try again.";
				} else if (reason.status === 404) {
					options.Body += '\n\nPlease ensure the specified pit exists and try again.';
				}
				else {
					console.log('JobService.StartJob().error>', reason);
					return;
				}

				Alert(this.$modal, options);
			});
			return StripHttpPromise(this.$q, promise);
		}

		public Delete(job: IJob): ng.IPromise<any> {
			return this.$http.delete(job.jobUrl)
				.then(() => { return this.GetJobs(); })
			;
		}
		
		public Continue(): void {
			this.sendCommand(
				this.CanContinue, 
				JobStatus.ContinuePending, 
				this.job.commands.continueUrl);
		}

		public Pause(): void {
			this.sendCommand(
				this.CanPause, 
				JobStatus.PausePending, 
				this.job.commands.pauseUrl);
		}

		public Stop(): void {
			this.sendCommand(
				this.CanStop, 
				JobStatus.StopPending, 
				this.job.commands.stopUrl);
		}
		
		public Kill(): void {
			this.sendCommand(
				this.CanStop,
				JobStatus.KillPending,
				this.job.commands.killUrl);
		}

		private sendCommand(check: boolean, status: string, url: string): void {
			if (check) {
				this.job.status = status;
				this.$http.get(url)
					.success(() => this.onPoll(this.job.jobUrl))
					.error(reason => this.onError(reason));
			}
		}

		private onError(error: any): void {
			console.log('onError', error);
		}

		private onPoll(url: string): void {
			this.pending = this.$http.get(url)
				.then((response: ng.IHttpPromiseCallbackArg<IJob>) => {
					if (!this.isActive)
						return undefined;

					var stopPending = (this.job && this.job.status === JobStatus.StopPending);
					var killPending = (this.job && this.job.status === JobStatus.KillPending);

					var job = response.data;
					this.job = job;

					if (this.job.status !== JobStatus.Stopped) {
						if (stopPending && this.job.status !== JobStatus.Stopping) {
							this.job.status = JobStatus.StopPending;
						} else if (killPending) {
							this.job.status = JobStatus.KillPending;
						}
					}

					this.$rootScope['job'] = this.job;

					if (job.status !== JobStatus.Stopped) {
						this.poller = this.$timeout(() => { this.onPoll(url); }, JOB_INTERVAL);
					}

					if (this.faults.length !== job.faultCount) {
						var deferred = this.$q.defer<IJob>();
						this.reloadFaults()
							.success(() => { deferred.resolve(this.job); })
							.error(reason => { deferred.reject(reason); })
							.finally(() => { this.pending = undefined; })
						;
						return deferred.promise;
					}

					return undefined;
				},(response: ng.IHttpPromiseCallbackArg<IError>) => {
					if (!this.isActive)
						return undefined;
					this.$state.go(C.States.MainError, { message: response.data.errorMessage });
				})
			;
		}

		private reloadFaults(): ng.IHttpPromise<IFaultSummary[]> {
			return this.$http.get(this.job.faultsUrl)
				.success((faults: IFaultSummary[]) => {
					this.faults = faults;
				})
				.error(reason => this.onError(reason));
		}

		public LoadMetric<T>(metric: string): ng.IHttpPromise<T> {
			return this.$http.get(this.Job.metrics[metric]);
		}
	}
}
