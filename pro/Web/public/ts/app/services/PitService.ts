﻿/// <reference path="../reference.ts" />

namespace Peach {
	export class PitService {

		static $inject = [
			C.Angular.$rootScope,
			C.Angular.$q,
			C.Angular.$http,
			C.Angular.$state
		];

		constructor(
			private $rootScope: ng.IRootScopeService,
			private $q: ng.IQService,
			private $http: ng.IHttpService,
			private $state: ng.ui.IStateService
		) {
		}

		private pit: IPit;
		private userPitLibrary: string;

		public get CurrentPitId() {
			return this.$state.params['pit'];
		}

		public get Pit(): IPit {
			return this.pit;
		}

		public LoadLibrary(): ng.IPromise<ILibrary[]> {
			const promise = this.$http.get(C.Api.Libraries);
			promise.success((libs: ILibrary[]) => {
				this.userPitLibrary = _(libs)
					.reject({ locked: true })
					.first()
					.libraryUrl;
			});
			promise.catch((reason: ng.IHttpPromiseCallbackArg<IError>) => {
				this.$state.go(C.States.MainError, { message: reason.data.errorMessage });
			});
			return StripHttpPromise(this.$q, promise);
		}

		public LoadPit(): ng.IPromise<IPit> {
			const url = C.Api.PitUrl.replace(':id', this.CurrentPitId);
			const promise = this.$http.get(url);
			promise.success((pit: IPit) => this.OnSuccess(pit, false));
			promise.catch((reason: ng.IHttpPromiseCallbackArg<IError>) => {
				this.$state.go(C.States.MainError, { message: reason.data.errorMessage });
			});
			return StripHttpPromise(this.$q, promise);
		}

		private SavePit(): ng.IPromise<IPit> {
			const config: IParameter[] = [];
			const view = this.CreateFlatDefinesView(this.pit.config);
			for (let param of view) {
				if (param.type === ParameterType.User) {
					config.push({
						name: param.name,
						description: param.description,
						key: param.key,
						value: param.value
					});
				} else if (param.type !== ParameterType.System) {
					config.push({
						key: param.key,
						value: param.value
					});
				}
			}

			const agents: IAgent[] = [];
			for (let agent of this.pit.agents) {
				const monitors: IMonitor[] = [];
				for (let monitor of agent.monitors) {
					var map: IParameter[] = [];
					this.Visit(monitor.view, (param: IParameter) => {
						if (!_.isUndefined(param.value)) {
							map.push({
								key: param.key,
								value: param.value
							});
						}
					});
					monitors.push({
						monitorClass: monitor.monitorClass,
						name: monitor.name,
						map: map
					});
				}
				agents.push({
					name: agent.name,
					agentUrl: agent.agentUrl,
					monitors: monitors
				});
			}

			const dto: IPit = {
				id: this.pit.id,
				pitUrl: this.pit.pitUrl,
				name: this.pit.name,
				config: config,
				agents: agents
			};

			const promise = this.$http.post(this.pit.pitUrl, dto);
			promise.success((pit: IPit) => this.OnSuccess(pit, true));
			return StripHttpPromise(this.$q, promise);
		}

		public SaveVars(config: IParameter[]): ng.IPromise<IPit> {
			this.pit.config = config;
			return this.SavePit();
		}

		public SaveAgents(agents: IAgent[]): ng.IPromise<IPit> {
			this.pit.agents = agents;
			return this.SavePit();
		}

		public SaveConfig(pit: IPit): ng.IHttpPromise<IPit> {
			const request: IPitCopy = {
				libraryUrl: this.userPitLibrary,
				pitUrl: pit.pitUrl,
				name: pit.name,
				description: pit.description
			};
			const promise = this.$http.post(C.Api.Pits, request);
			promise.success((pit: IPit) => this.OnSuccess(pit, true));
			return promise;
		}

		public get IsConfigured(): boolean {
			return onlyIf(this.pit, () => _.all(this.CreateFlatDefinesView(this.CreateDefinesView()), (param: IParameter) => {
				return param.optional || param.value !== "";
			}));
		}

		public get HasMonitors(): boolean {
			return onlyIf(this.pit, () => _.any(this.pit.agents, (agent: IAgent) => {
				return agent.monitors.length > 0;
			}));
		}

		private OnSuccess(pit: IPit, saved: boolean) {
			const oldPit = this.pit;
			this.pit = pit;
			this.$rootScope['pit'] = pit;
			if (saved || (oldPit && oldPit.id !== pit.id)) {
				this.$rootScope.$emit(C.Events.PitChanged, pit);
			}

			for (let agent of pit.agents) {
				for (let monitor of agent.monitors) {
					monitor.view = this.CreateMonitorView(monitor);
				}
			}

			if (pit.metadata) {
				pit.definesView = this.CreateDefinesView();
			}
		}

		public CreateMonitor(param: IParameter): IMonitor {
			const monitor: IMonitor = {
				monitorClass: param.key,
				name: param.name,
				map: angular.copy(param.items),
				description: param.description
			};
			monitor.view = this.CreateMonitorView(monitor);
			return monitor;
		}

		private CreateDefinesView(): IParameter[] {
			const view = angular.copy(this.pit.metadata.defines);
			for (let group of view) {
				for (let param of group.items) {
					const config = _.find(this.pit.config, { key: param.key });
					if (config && config.value) {
						param.value = config.value;
					}
				}
			}
			return view;
		}

		private CreateFlatDefinesView(src: IParameter[]): IParameter[] {
			const skip = [
				ParameterType.Group,
				ParameterType.Monitor,
				ParameterType.Space
			];
			const view: IParameter[] = [];
			this.Visit(src, (param: IParameter) => {
				if (_.contains(skip, param.type)) {
					return;
				}
				view.push(param);
			});
			return view;
		}

		public CreateMonitorView(monitor: IMonitor): IParameter[] {
			const metadata = this.FindMonitorMetadata(monitor.monitorClass);
			const view = angular.copy(metadata.items);
			this.Visit(view, (param: IParameter) => {
				const kv = _.find(monitor.map, { key: param.key });
				if (kv && kv.value) {
					param.value = kv.value;
				}
			});
			return view;
		}

		private FindMonitorMetadata(key: string): IParameter {
			for (let monitor of this.pit.metadata.monitors) {
				const ret = this._FindByTypeKey(monitor, ParameterType.Monitor, key);
				if (ret) {
					return ret;
				}
			}
			return null;
		}

		private _FindByTypeKey(param: IParameter, type: string, key: string): IParameter {
			if (param.type === type) {
				if (param.key === key) {
					return param;
				}
			}

			for (let item of _.get<IParameter[]>(param, 'items', [])) {
				const ret = this._FindByTypeKey(item, type, key);
				if (ret) {
					return ret;
				}
			}

			return null;
		}

		private Visit(params: IParameter[], fn: (p: IParameter) => void): void {
			for (let param of params) {
				fn(param);
				this.Visit(param.items || [], fn);
			}
		}
	}
}
