
module DashApp.Services {
	"use strict";

	import W = Models.Wizard;
	import P = Models.Peach;
	

	export interface IPitConfiguratorService {
		Pit: Models.Peach.Pit;
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

		public Pit: Models.Peach.Pit;
		public QA: W.Question[] = [];
		public Monitors: W.Monitor[] = [];

		//#region StateBag
		private _stateBag: W.StateBag = new W.StateBag();

		public get StateBag(): W.StateBag {
			return this._stateBag;
		}

		public set StateBag(stateBag: W.StateBag) {
			if (this._stateBag != stateBag)
				this.StateBag = stateBag;
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
			if (this._dataMonitors != monitors)
				this._dataMonitors = monitors;
		}
		//#endregion

		//#region AutoMonitors
		private _autoMonitors: W.Agent[] = [];
		public get AutoMonitors(): W.Agent[] {
			return this._autoMonitors;
		}

		public set AutoMonitors(monitors: W.Agent[]) {
			if (this._autoMonitors != monitors)
				this._autoMonitors = monitors;
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

		public LoadData(data) {
			if (data.config != undefined) {
				this._defines = new P.PitConfig(<P.PitConfig>data);
				this.QA = this._defines.ToQuestions();
			}
			else {
			if (data.qa != undefined)
				this.QA = <W.Question[]>data.qa;

				if (data.state != undefined) {
					this._stateBag = new W.StateBag(<any[]>data.state);
				}

				if (data.monitors != undefined)
					this.Monitors = <W.Monitor[]>data.monitors;
			}
		}
	}
}