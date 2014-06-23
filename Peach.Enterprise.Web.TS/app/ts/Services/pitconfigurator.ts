
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

		SetVarsComplete: boolean;
		FaultMonitorsComplete: boolean;
		DataMonitorsComplete: boolean;
		AutoMonitorsComplete: boolean;
		
		ResetAll();
		LoadData(data);
	}

	export class PitConfiguratorService implements IPitConfiguratorService {
		private jobPoller;
		private faultsPoller;
		private pollerSvc;
		private peachSvc: Services.IPeachService;

		private POLLER_TIME = 5000;

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
				this.getPitInfo();
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
				this._pit = pit;
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

		public get SetVarsComplete(): boolean {
			return (this._defines != undefined);
		}

		public get FaultMonitorsComplete(): boolean {
			return (this._faultMonitors.length > 0);
		}

		public get DataMonitorsComplete(): boolean {
			return (this._dataMonitors.length > 0);
		}

		public get AutoMonitorsComplete(): boolean {
			return (this._autoMonitors.length > 0);
		}

		public ResetAll() {
			this._defines = undefined;
			this._faultMonitors = [];
			this._dataMonitors = [];
			this._autoMonitors = [];

			// Hmm, I dunno...
			//this._stateBag = new W.StateBag;
		}

		public CopyPit(pit: P.Pit) {
			var request: P.CopyPitRequest;
			request.libraryUrl = this.UserPitLibrary;
			request.pit = pit;

			this.peachSvc.CopyPit(request, (data: P.Pit) => {
				this.Pit = data;
			});
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

		private startJobPoller() {
			var jobResource = this.peachSvc.GetSingleThing(this.Job.jobUrl);
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

		private updateJob(job: P.Job) {
			this.Job.iterationCount = job.iterationCount;
			this.Job.speed = job.speed;
			this.Job.faultCount = job.faultCount;
			this.Job.runtime = job.runtime;
		}

		private startFaultsPoller() {
			var faultsResource = this.peachSvc.GetManyThings(this._job.faultsUrl);
			this.faultsPoller = this.pollerSvc.get(faultsResource, {
				action: "get",
				delay: this.POLLER_TIME,
				method: "GET"
			});

			this.faultsPoller.promise.then(null, (e) => {
				console.error(e);
			}, (data: P.Fault[]) => {
				this.Faults = data;
				});
		}


		private getPitInfo() {
			if (this.Pit == undefined || (this.Pit.name != this._job.name)) {
				if (this._job.pitUrl.length > 0)
					this.peachSvc.GetPit(this._job.pitUrl, (data: P.Pit) => {
						if (data != undefined)
							this.Pit = data;
					});
				else {
					this.Pit = new P.Pit();
					var lastSlash = this._job.name.lastIndexOf("\\");
					var period = this._job.name.lastIndexOf(".");
					this.Pit.name = this._job.name.substring(lastSlash + 1, period);
					this.Pit.description = "";
					this.Pit.tags = [];
				}
			}
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