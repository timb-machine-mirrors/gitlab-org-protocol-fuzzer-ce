/// <reference path="../reference.ts" />

module DashApp.Services {
	"use strict";

	export interface IPitConfiguratorService {
		Job: Models.IJob;
		Pit: Models.Pit;
		Faults: Models.IFault[];

		UserPitLibrary: string;
		QA: Models.Question[];
		State: Models.NameValueCollection<string>;
		Defines: Models.PitConfig;
		Monitors: Models.IMonitor[];
		FaultMonitors: Models.Agent[];
		DataMonitors: Models.Agent[];
		AutoMonitors: Models.Agent[];

		TestEvents: Models.ITestEvent[];
		TestStatus: string;
		TestLog: string;
		TestTime: string;

		SetVarsComplete: boolean;
		FaultMonitorsComplete: boolean;
		DataMonitorsComplete: boolean;
		AutoMonitorsComplete: boolean;
		IntroComplete: boolean;
		TestComplete: boolean;
		DoneComplete: boolean;

		CanStartJob: boolean;
		CanPauseJob: boolean;
		CanContinueJob: boolean;
		CanStopJob: boolean;


		ResetAll();
		ResetTestData();
		LoadData(data);

		StartJob();
		StartJob(job: Models.IJob);
		PauseJob();
		StopJob();

		InitializeSetVars();
		InitializeState();
	}

	export class PitConfiguratorService implements IPitConfiguratorService {
		private jobPoller;
		private faultsPoller;
		private pollerSvc;
		private peachSvc: Services.IPeachService;

		private POLLER_TIME = 500;

		constructor(poller, peachService: Services.IPeachService) {
			this.pollerSvc = poller;
			this.peachSvc = peachService;

			this.initialize();
		}

		public Faults: Models.IFault[] = [];
		public Monitors: Models.IMonitor[] = [];
		public UserPitLibrary: string;

		private _qa: Models.Question[] = [];
		public get QA() {
			return this._qa;
		}

		//#region Job
		private _job: Models.IJob;

		public get Job(): Models.IJob {
			return this._job;
		}

		public set Job(job: Models.IJob) {
			if (this._job != job) {
				this._job = job;
				this.startJobPoller();
				this.startFaultsPoller();
				this.getPit(job.pitUrl);
			}
		}
		//#endregion

		//#region Pit
		private _pit: Models.Pit;

		public get Pit(): Models.Pit {
			return this._pit;
		}

		public set Pit(pit: Models.Pit) {
			if (this._pit != pit) {
				if (pit.hasVersion == undefined) {
					this._pit = new Models.Pit(pit);
				} else {
					this._pit = pit;
				}
				this.ResetAll();

				if (pit.pitUrl != undefined) {
					this.peachSvc.GetDefines(pit.pitUrl).get((data : Models.PitConfig) => {
						this.Defines = new Models.PitConfig(data);
					});
				}
			}
		}
		//#endregion

		public State: Models.NameValueCollection<any> = {};
		public Defines: Models.PitConfig;
		public FaultMonitors: Models.Agent[] = [];
		public DataMonitors: Models.Agent[] = [];
		public AutoMonitors: Models.Agent[] = [];

		public IntroComplete: boolean = false;
		public SetVarsComplete: boolean = false;
		public FaultMonitorsComplete: boolean = false;
		public DataMonitorsComplete: boolean = false;
		public AutoMonitorsComplete: boolean = false;
		public TestComplete: boolean = false;
		public DoneComplete: boolean = false;
		
		public TestEvents: Models.ITestEvent[] = [];
		public TestStatus: string = "notrunning";
		public TestLog: string = "";
		public TestTime: string = "";

		public ResetAll() {
			this.ResetTestData();
			this.ResetConfiguratorSteps();
		}

		public ResetConfiguratorSteps() {
			this.IntroComplete = false;
			this.SetVarsComplete = false;
			this.FaultMonitorsComplete = false;
			this.DataMonitorsComplete = false;
			this.AutoMonitorsComplete = false;
			this.TestComplete = false;
			this.DoneComplete = false;
		}

		public ResetTestData() {
			this.TestEvents = [];
			this.TestStatus = "notrunning";
			this.TestLog = "";
			this.TestTime = "";
		}

		public LoadData(data) {
			if (data.qa != undefined)
				this._qa = Models.Question.CreateQA(<Models.Question[]>data.qa);

			if (data.monitors != undefined)
				this.Monitors = <Models.IMonitor[]>data.monitors;
		}

		public get CanStartJob(): boolean {
			var good: string[] = [Models.JobStatuses.Stopped];
			return (
				(this._job == undefined && this.isKnownPit) ||
				(this._job !== undefined && good.indexOf(this._job.status) >= 0)
			);
		}

		public get CanContinueJob(): boolean {
			var good: string[] = [Models.JobStatuses.Paused];
			return (
				this._job != undefined &&
				good.indexOf(this._job.status) >= 0 &&
				this.isKnownPit
			);
		}

		public get CanPauseJob(): boolean {
			var good: string[] = [Models.JobStatuses.Running];
			return (
				this._job != undefined &&
				good.indexOf(this._job.status) >= 0 &&
				this.isKnownPit
			);
		}

		public get CanStopJob(): boolean {
			var good: string[] = [
				Models.JobStatuses.Running,
				Models.JobStatuses.Paused,
				Models.JobStatuses.StartPending,
				Models.JobStatuses.PausePending,
				Models.JobStatuses.ContinuePending
			];
			return (
				this._job != undefined &&
				good.indexOf(this._job.status) >= 0 &&
				this.isKnownPit
			);
		}

		private get isKnownPit() {
			return (
				this.Pit != undefined &&
				this.Pit.pitUrl != undefined &&
				this.Pit.pitUrl.length > 0 &&
				this.Pit.configured
			);
		}

		public StartJob(job?: Models.IJob) {
			if (this.CanStartJob) {
				if (job === undefined) {
					this.peachSvc.StartJob(
						{ pitUrl: this.Pit.pitUrl },
						(job: Models.IJob) => this.StartJobSuccess(job),
						(response) => this.StartJobFail
					);
				} else {
					job.pitUrl = this.Pit.pitUrl;
					this.peachSvc.StartJob(
						job,
						(job: Models.IJob) => this.StartJobSuccess(job),
						(response) => this.StartJobFail
					);
				}
			}
			else if (this.CanContinueJob) {
				this._job.status = Models.JobStatuses.ActionPending;
				this.peachSvc.ContinueJob(this._job.jobUrl);
			}
		}

		private StartJobSuccess(job: Models.IJob) {
			this.Job = job;
		}

		private StartJobFail(response) {
			alert("Peach is busy with another task. Can't create Job.\nConfirm that there aren't multiple browsers accessing the same instance of Peach.");
			this._job = undefined;
		}

		public PauseJob() {
			if (this.CanPauseJob) {
				this._job.status = Models.JobStatuses.ActionPending;
				this.peachSvc.PauseJob(this._job.jobUrl);
			}
		}

		public StopJob() {
			if (this.CanStopJob) {
				this._job.status = Models.JobStatuses.ActionPending;
				this.peachSvc.StopJob(this._job.jobUrl);
			}
		}

		private startJobPoller() {
			if (this.jobPoller == undefined) {
				var jobResource = this.peachSvc.GetJobResource(this.Job.jobUrl);
				this.jobPoller = this.pollerSvc.get(jobResource, {
					action: "get",
					delay: this.POLLER_TIME,
					method: "GET"
				});

				this.jobPoller.promise.then(null, (e) => {
					console.error(e);
				}, (data: Models.IJob) => {
						this.updateJob(data);
					});
			}
			else {
				throw "jobPoller, wasn't what I was expecting";
			}
		}

		private updateJob(job: Models.IJob) {
			if (this.Job.jobUrl == undefined) {
				this.Job.jobUrl = job.jobUrl;
			} else {
				if (this.Job.jobUrl != job.jobUrl) {
					throw "tried to update job with a different joburl";
				}
			}

			this.Job.faultsUrl = job.faultsUrl;
			this.Job.iterationCount = job.iterationCount;
			this.Job.speed = job.speed;
			this.Job.runtime = job.runtime;
			this.Job.status = job.status;
			this.Job.startDate = job.startDate;
			this.Job.stopDate = job.stopDate;
			this.Job.seed = job.seed;

			this.Job.faultCount = job.faultCount;

			if (job.status == Models.JobStatuses.Stopped && this.jobPoller != undefined) {
				this.jobPoller.stop();
				this.jobPoller = undefined;
				this.faultsPoller.stop();
				this.faultsPoller = undefined;
			}
		}

		private startFaultsPoller() {
			if (this.faultsPoller == undefined && this._job.faultsUrl !== undefined) {
				var faultsResource = this.peachSvc.GetManyResources(this._job.faultsUrl);
				this.faultsPoller = this.pollerSvc.get(faultsResource, {
					action: "get",
					delay: 1000,
					method: "GET"
				});

				this.faultsPoller.promise.then(null, (e) => {
					console.error(e);
				}, (data: Models.IFault[]) => {
					this.Faults = data;
				});
			}
			else {
				console.error("uh...");
			}
		}

		public getPit(pitUrl: string) {
			if (this.Pit == undefined || (this.Pit.name != this._job.name)) {
				if (this._job.pitUrl.length > 0)
					this.peachSvc.GetPit(this._job.pitUrl, (data: Models.Pit) => {
						if (data != undefined) {
							if ((this.Pit == undefined) || (this.Pit.pitUrl != data.pitUrl))
								this.Pit = new Models.Pit(data);
							else {
								this.updatePit(data);
							}
						}
					});
				else {
					this.Pit = new Models.Pit();
					this.Pit.name = this._job.name;
					this.Pit.pitUrl = "";
					this.Pit.description = "";
					this.Pit.tags = [];
					this.Pit.versions = [{
						version: 1,
						configured: true,
						locked: true
					}];
				}
			}
		}

		public InitializeSetVars() {
			this._qa = this.Defines.ToQuestions();
		}

		private updatePit(pit: Models.Pit) {
			if (this.Pit.pitUrl != pit.pitUrl) {
				throw "trying to update a pit with the wrong pit url";
			}

			this.Pit.configured = pit.configured;
			this.Pit.description = pit.description;
			this.Pit.locked = pit.locked;
			this.Pit.name = pit.name;
			this.Pit.tags = pit.tags;
		}

		private initialize() {
			this.getUserLibrary();
			this.InitializeState();
		}

		private getUserLibrary() {
			this.peachSvc.GetLibraries((data: Models.IPitLibrary[]) => {
				var libs: Models.IPitLibrary[] = $.grep(data, (e) => {
					return e.locked == false;
				});
				this.UserPitLibrary = libs[0].libraryUrl;
			});
		}

		public InitializeState() {
			this.peachSvc.GetState((data: Models.IPair<string, string>[]) => {
				for (var i = 0; i < data.length; i++) {
					this.State[data[i].key] = data[i].value;
				}
			});
		}
	}
}
