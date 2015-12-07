/// <reference path="../reference.ts" />

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
			for (let param of this.pit.config) {
				if (param.type === ParameterType.Group) {
					for (let item of param.items) {
						if (item.value && item.type !== ParameterType.System) {
							config.push({
								key: item.key,
								value: item.value
							});
						}
					}
				} else {
					if (param.value && param.type !== ParameterType.System) {
						config.push({
							key: param.key,
							value: param.value
						});
					}
				}
			}

			const agents: IAgent[] = [];
			for (let agent of _.get<IAgent[]>(this.pit, 'agents', [])) {
				const monitors: IMonitor[] = [];
				for (let monitor of agent.monitors) {
					var map: IParameter[] = [];
					if (monitor.view) {
						this._Visit(monitor.view, (param: IParameter) => {
							if (!_.isUndefined(param.value)) {
								map.push({
									key: param.key,
									value: param.value
								});
							}
						});
					} else {
						for (let param of monitor.map) {
							map.push({
								key: param.key,
								value: param.value
							});
						}
					}
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
			return onlyIf(this.pit, () => _.all(this.pit.config, (x: IParameter) => {
				return x.optional || x.value !== "";
			}));
		}

		public get HasMonitors(): boolean {
			return onlyIf(this.pit, () => _.any(this.pit.agents, (x: IAgent) => {
				return x.monitors.length > 0;
			}));
		}

		private OnSuccess(pit: IPit, saved: boolean) {
			const oldPit = this.pit;
			this.pit = pit;
			this.$rootScope['pit'] = pit;
			if (saved || (oldPit && oldPit.id !== pit.id)) {
				this.$rootScope.$emit(C.Events.PitChanged, pit);
			}

			for (let agent of _.get<IAgent[]>(pit, 'agents', [])) {
				for (let monitor of _.get<IMonitor[]>(agent, 'monitors', [])) {
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

		private CreateMonitorView(monitor: IMonitor): IParameter {
			const metadata = this.FindMonitorMetadata(monitor.monitorClass);
			const view = angular.copy(metadata);
			this._Visit(view, (param: IParameter) => {
				if (param.type === ParameterType.Monitor) {
					return;
				}
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

		private _Visit(param: IParameter, fn: (p: IParameter) => void): void {
			fn(param);

			for (let item of _.get<IParameter[]>(param, 'items', [])) {
				this._Visit(item, fn);
			}
		}
	}
}
