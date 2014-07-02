
module DashApp.Services {
	"use strict";

	import W = Models.Wizard;
	import P = Models.Peach;
	

	export interface IPitConfiguratorService {
		Job: Models.Peach.Job;
		Pit: Models.Peach.Pit;
		Faults: Models.Peach.Fault[];

		UserPitLibrary: string;
		QA: W.Question[];
		StateBag: W.StateBag;
		Defines: P.PitConfig;
		Monitors: W.Monitor[];
		FaultMonitors: W.Agent[];
		DataMonitors: W.Agent[];
		AutoMonitors: W.Agent[];

		TestEvents: P.TestEvent[];
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
		PauseJob();
		StopJob();

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

		public Faults: Models.Peach.Fault[] = [];
		public QA: W.Question[] = [];
		public Monitors: W.Monitor[] = [];
		public UserPitLibrary: string;

		//#region Job
		private _job: Models.Peach.Job;

		public get Job(): P.Job {
			return this._job;
		}

		public set Job(job: P.Job) {
			if (this._job != job) {
				this._job = job;
				this.startJobPoller();
				this.startFaultsPoller();

				this.getPit(job.pitUrl);
			}
		}
		//#endregion

		//#region StateBag
		private _stateBag: W.StateBag = new W.StateBag();

		public get StateBag(): W.StateBag {
			return this._stateBag;
		}

		public set StateBag(stateBag: W.StateBag) {
			if (this._stateBag != stateBag)
				this._stateBag = stateBag;
		}
		//#endregion

		//#region Defines
		private _defines: P.PitConfig;

		public get Defines(): P.PitConfig {
			return this._defines;
		}

		public set Defines(defines: P.PitConfig) {
			if (this._defines != defines) {
				this._defines = defines;
			}
		}
		//#endregion

		//#region Pit
		private _pit: P.Pit;

		public get Pit(): P.Pit {
			return this._pit;
		}

		public set Pit(pit: P.Pit) {
			if (this._pit != pit) {
				if (pit.hasVersion == undefined) {
					this._pit = new P.Pit(pit);
				} else {
					this._pit = pit;
				}
				this.ResetAll();

				if (pit.pitUrl != undefined) {
					this.peachSvc.GetDefines(pit.pitUrl).get((data) => {
						this._defines = new P.PitConfig(<P.PitConfig>data);
					});
				}
			}
		}
		//#endregion

		//#region FaultMonitors
		private _faultMonitors: W.Agent[] = [];

		public get FaultMonitors(): W.Agent[] {
			return this._faultMonitors;
		}

		public set FaultMonitors(monitors: W.Agent[]) {
			if (this._faultMonitors != monitors) {
				this._faultMonitors = monitors;
			}
		}
		//#endregion

		//#region DataMonitors
		private _dataMonitors: W.Agent[] = [];
		public get DataMonitors(): W.Agent[] {
			return this._dataMonitors;
		}

		public set DataMonitors(monitors: W.Agent[]) {
			if (this._dataMonitors != monitors) {
				this._dataMonitors = monitors;
			}
		}
		//#endregion

		//#region AutoMonitors
		private _autoMonitors: W.Agent[] = [];
		public get AutoMonitors(): W.Agent[] {
			return this._autoMonitors;
		}

		public set AutoMonitors(monitors: W.Agent[]) {
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


		public TestEvents: P.TestEvent[] = [];
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
			//this._stateBag = new W.StateBag;
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
				this.QA = <W.Question[]>data.qa;

			if (data.state != undefined) {
				this._stateBag = new W.StateBag(<any[]>data.state);
			}

			if (data.monitors != undefined)
				this.Monitors = <W.Monitor[]>data.monitors;

		}

		public get CanStartJob(): boolean {
			var good: string[] = [P.JobStatuses.Stopped];
			return (((this._job == undefined) && (this.isKnownPit)) || ((this._job != undefined) && (good.indexOf(this._job.status) >= 0)));
		}

		public get CanContinueJob(): boolean {
			var good: string[] = [P.JobStatuses.Paused];
			return ((this._job != undefined) && (good.indexOf(this._job.status) >= 0) && this.isKnownPit);
		}

		public get CanPauseJob(): boolean {
			var good: string[] = [P.JobStatuses.Running];
			return ((this._job != undefined) && (good.indexOf(this._job.status) >= 0) && this.isKnownPit);
		}

		public get CanStopJob(): boolean {
			var good: string[] = [P.JobStatuses.Running, P.JobStatuses.Paused, P.JobStatuses.StartPending, P.JobStatuses.PausePending, P.JobStatuses.ContinuePending];
			return ((this._job != undefined) && (good.indexOf(this._job.status) >= 0) && this.isKnownPit);
		}

		private get isKnownPit() {
			return (this.Pit != undefined && this.Pit.pitUrl != undefined && this.Pit.pitUrl.length > 0 && this.Pit.configured);
		}


		public StartJob() {
			if (this.CanStartJob) {
				this.peachSvc.StartJob(this.Pit.pitUrl, (job: P.Job) => {
					this.Job = job;
				}, (response) => {
					alert("Peach is busy with another task. Can't create Job.\nConfirm that there aren't multiple browsers accessing the same instance of Peach.");
					this._job = undefined;
				});
			}
			else if (this.CanContinueJob) {
				this._job.status = P.JobStatuses.ActionPending;
				this.peachSvc.ContinueJob(this._job.jobUrl);
			}
		}

		public PauseJob() {
			if (this.CanPauseJob) {
				this._job.status = P.JobStatuses.ActionPending;
				this.peachSvc.PauseJob(this._job.jobUrl);
			}
		}

		public StopJob() {
			if (this.CanStopJob) {
				this._job.status = P.JobStatuses.ActionPending;
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
				}, (data: P.Job) => {
						this.updateJob(data);
					});
			}
			else {
				throw "jobPoller, wasn't what I was expecting";
			}
		}

		private updateJob(job: P.Job) {
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


			if (job.status == P.JobStatuses.Stopped && this.jobPoller != undefined) {
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
				}, (data: P.Fault[]) => {
					//var start: number = 0;
					//if (this.Faults.length > 1) {
					//	start = this.Faults.length - 1;
					//}
					//for (var f = start; f < data.length; f++) {
					//	this.Faults.push(data[f]);
					//}
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
					this.peachSvc.GetPit(this._job.pitUrl, (data: P.Pit) => {
						if (data != undefined) {
							if ((this.Pit == undefined) || (this.Pit.pitUrl != data.pitUrl))
								this.Pit = new P.Pit(data);
							else {
								this.updatePit(data);
							}
						}
					});
				else {
					this.Pit = new P.Pit();
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

		private updatePit(pit: P.Pit) {
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
			//this.peachSvc.GetLocalFile<any>("../testdata/test_config.json", (data) => {
			//	this.peachSvc.URL_PREFIX = data.URL_PREFIX;
			//	this.getUserLibrary();
			//}, () => {
			//	this.getUserLibrary();
			//});

			this.getUserLibrary();
		}

		private getUserLibrary() {
			this.peachSvc.GetLibraries((data: P.PitLibrary[]) => {
				this.UserPitLibrary = $.grep(data, (e) => {
					return e.locked == false;
				})[0].libraryUrl;
			});
		}
	}
}