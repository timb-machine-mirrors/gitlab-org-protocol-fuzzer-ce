/// <reference path="../Models/models.ts" />
/// <reference path="peach.ts" />

module DashApp.Services {
	"use strict";

	

	export interface IPitConfiguratorService {
		Job: Models.Job;
		Pit: Models.Pit;
		Faults: Models.Fault[];

		UserPitLibrary: string;
		QA: Models.Question[];
		StateBag: Models.StateBag;
		Defines: Models.PitConfig;
		Monitors: Models.Monitor[];
		FaultMonitors: Models.Agent[];
		DataMonitors: Models.Agent[];
		AutoMonitors: Models.Agent[];

		TestEvents: Models.TestEvent[];
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
		StartJob(job: Models.Job);
		PauseJob();
		StopJob();

		InitializeSetVars();
		InitializeStateBag();
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

		public Faults: Models.Fault[] = [];
		public Monitors: Models.Monitor[] = [];
		public UserPitLibrary: string;


		private _qa: Models.Question[] = [];
		public get QA() {
			return this._qa;
		}

		//#region Job
		private _job: Models.Job;

		public get Job(): Models.Job {
			return this._job;
		}

		public set Job(job: Models.Job) {
			if (this._job != job) {
				this._job = job;
				this.startJobPoller();
				this.startFaultsPoller();

				this.getPit(job.pitUrl);
			}
		}
		//#endregion

		//#region StateBag
		private _stateBag: Models.StateBag = new Models.StateBag();

		public get StateBag(): Models.StateBag {
			return this._stateBag;
		}

		public set StateBag(stateBag: Models.StateBag) {
			if (this._stateBag != stateBag)
				this._stateBag = stateBag;
		}
		//#endregion

		//#region Defines
		private _defines: Models.PitConfig;

		public get Defines(): Models.PitConfig {
			return this._defines;
		}

		public set Defines(defines: Models.PitConfig) {
			if (this._defines != defines) {
				this._defines = defines;
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
					this.peachSvc.GetDefines(pit.pitUrl).get((data) => {
						this._defines = new Models.PitConfig(<Models.PitConfig>data);
					});
				}
			}
		}
		//#endregion

		//#region FaultMonitors
		private _faultMonitors: Models.Agent[] = [];

		public get FaultMonitors(): Models.Agent[] {
			return this._faultMonitors;
		}

		public set FaultMonitors(monitors: Models.Agent[]) {
			if (this._faultMonitors != monitors) {
				this._faultMonitors = monitors;
			}
		}
		//#endregion

		//#region DataMonitors
		private _dataMonitors: Models.Agent[] = [];
		public get DataMonitors(): Models.Agent[] {
			return this._dataMonitors;
		}

		public set DataMonitors(monitors: Models.Agent[]) {
			if (this._dataMonitors != monitors) {
				this._dataMonitors = monitors;
			}
		}
		//#endregion

		//#region AutoMonitors
		private _autoMonitors: Models.Agent[] = [];
		public get AutoMonitors(): Models.Agent[] {
			return this._autoMonitors;
		}

		public set AutoMonitors(monitors: Models.Agent[]) {
			if (this._autoMonitors != monitors) {
				this._autoMonitors = monitors;
			}
		}
		//#endregion

		public IntroComplete: boolean = false;
		public SetVarsComplete: boolean = false;
		public FaultMonitorsComplete: boolean = false;
		public DataMonitorsComplete: boolean = false;
		public AutoMonitorsComplete: boolean = false;
		public TestComplete: boolean = false;
		public DoneComplete: boolean = false;


		public TestEvents: Models.TestEvent[] = [];
		public TestStatus: string = "notrunning";
		public TestLog: string = "";
		public TestTime: string = "";

		
		public ResetAll() {
			this._defines = undefined;
			this._faultMonitors = [];
			this._dataMonitors = [];
			this._autoMonitors = [];
			this.ResetTestData();
			this.ResetConfiguratorSteps();
			// Hmm, I dunno...
			//this._stateBag = new Models.StateBag;
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
			if (data.qa != undefined) {
				this._qa = Models.Question.CreateQA(<Models.Question[]>data.qa); 
			}

			if (data.monitors != undefined)
				this.Monitors = <Models.Monitor[]>data.monitors;

		}

		public get CanStartJob(): boolean {
			var good: string[] = [Models.JobStatuses.Stopped];
			return (((this._job == undefined) && (this.isKnownPit)) || ((this._job != undefined) && (good.indexOf(this._job.status) >= 0)));
		}

		public get CanContinueJob(): boolean {
			var good: string[] = [Models.JobStatuses.Paused];
			return ((this._job != undefined) && (good.indexOf(this._job.status) >= 0) && this.isKnownPit);
		}

		public get CanPauseJob(): boolean {
			var good: string[] = [Models.JobStatuses.Running];
			return ((this._job != undefined) && (good.indexOf(this._job.status) >= 0) && this.isKnownPit);
		}

		public get CanStopJob(): boolean {
			var good: string[] = [Models.JobStatuses.Running, Models.JobStatuses.Paused, Models.JobStatuses.StartPending, Models.JobStatuses.PausePending, Models.JobStatuses.ContinuePending];
			return ((this._job != undefined) && (good.indexOf(this._job.status) >= 0) && this.isKnownPit);
		}

		private get isKnownPit() {
			return (this.Pit != undefined && this.Pit.pitUrl != undefined && this.Pit.pitUrl.length > 0 && this.Pit.configured);
		}
		
		public StartJob(job?: Models.Job) {
			if (this.CanStartJob) {
				if (job == undefined) {
					this.peachSvc.StartJob({ pitUrl: this.Pit.pitUrl }, (job: Models.Job) => this.StartJobSuccess(job), (response) => this.StartJobFail);
				} else {
					job.pitUrl = this.Pit.pitUrl;
					this.peachSvc.StartJob(job, (job: Models.Job) => this.StartJobSuccess(job), (response) => this.StartJobFail);
				}
			}
			else if (this.CanContinueJob) {
				this._job.status = Models.JobStatuses.ActionPending;
				this.peachSvc.ContinueJob(this._job.jobUrl);
			}
		}

		private StartJobSuccess(job: Models.Job) {
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
				}, (data: Models.Job) => {
						this.updateJob(data);
					});
			}
			else {
				throw "jobPoller, wasn't what I was expecting";
			}
		}

		private updateJob(job: Models.Job) {
			if (this.Job.jobUrl == undefined) {
				this.Job.jobUrl = job.jobUrl;
			}
			else {
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
			if (this.faultsPoller == undefined && this._job.faultsUrl != undefined) {
				var faultsResource = this.peachSvc.GetManyResources(this._job.faultsUrl);
				this.faultsPoller = this.pollerSvc.get(faultsResource, {
					action: "get",
					delay: 1000,
					method: "GET"
				});

				this.faultsPoller.promise.then(null, (e) => {
					console.error(e);
				}, (data: Models.Fault[]) => {
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
				return;
			}

			this.Pit.configured = pit.configured;
			this.Pit.description = pit.description;
			this.Pit.locked = pit.locked;
			this.Pit.name = pit.name;
			this.Pit.tags = pit.tags;
		}



		private initialize() {
			this.getUserLibrary();
			this.InitializeStateBag();
		}

		private getUserLibrary() {
			this.peachSvc.GetLibraries((data: Models.PitLibrary[]) => {
				this.UserPitLibrary = $.grep(data, (e) => {
					return e.locked == false;
				})[0].libraryUrl;
			});
		}

		public InitializeStateBag() {
			this.peachSvc.GetState((data: Models.StateItem[]) => {
				this.StateBag = new Models.StateBag(data);
			});

		}
	}
}