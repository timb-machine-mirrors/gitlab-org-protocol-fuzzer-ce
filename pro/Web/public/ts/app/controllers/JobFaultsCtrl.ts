/// <reference path="../reference.ts" />

namespace Peach {
	export interface IFaultFiles
	{
		// Is list of test i/o
		TestData: IFaultFile[];

		// Is list of Agents
		// Children is List if Monitors
		// Children[0].Children is files for a given monitor
		GroupedMonitorAssets: IFaultFile[];

		FlatMonitorAssets: IFaultFile[];

		// Is list of test i/o
		Other: IFaultFile[];
	}

	export interface IFaultDetailScope extends IFaultSummaryScope {
		FaultDetailTitle: string;
	}

	function FaultsTitle(bucket: string) {
		return (bucket === "all") ? 'Faults' : 'Bucket: ' + bucket;
	}

	export class FaultsDetailController {
		static $inject = [
			C.Angular.$scope,
			C.Angular.$state,
			C.Services.Job
		];

		constructor(
			$scope: IFaultDetailScope,
			$state: ng.ui.IStateService,
			jobService: JobService
		) {
			$scope.FaultSummaryTitle = FaultsTitle($state.params['bucket']);

			const id = $state.params['id'];
			$scope.FaultDetailTitle = 'Iteration: ' + id;
			const promise = jobService.LoadFaultDetail(id);
			promise.then((detail: IFaultDetail) => {
				this.Fault = detail;

				this.Initial = {
					TestData: [],
					GroupedMonitorAssets: [],
					FlatMonitorAssets: [],
					Other: []
				};

				this.Files = {
					TestData: [],
					GroupedMonitorAssets: [],
					FlatMonitorAssets: [],
					Other: []
				};

				detail.files.forEach((f: IFaultFile) => {
					if (f.initial)
						this.doAddFile(this.Initial, f);
					else
						this.doAddFile(this.Files, f);
				});
			}, () => {
				$state.go(C.States.MainHome);
			});
		}

		public Fault: IFaultDetail;
		public Files: IFaultFiles;
		public Initial: IFaultFiles;

		private doAddFile(dst: IFaultFiles, f: IFaultFile) {
			if (f.type === FaultFileType.Asset) {
				if (_.isEmpty(f.agentName) || _.isEmpty(f.monitorClass) || _.isEmpty(f.monitorName)) {
					dst.Other.push(f);

					if (f.initial)
						f.displayName = f.name;
					else
						f.displayName = f.fullName;
				} else {
					let lastAgent = _.last(dst.GroupedMonitorAssets);
					if (_.isUndefined(lastAgent) || lastAgent.agentName !== f.agentName) {
						lastAgent = {
							agentName: f.agentName,
							children: []
						}

						dst.GroupedMonitorAssets.push(lastAgent);
					}

					let lastMonitor = _.last(lastAgent.children);
					if (_.isUndefined(lastMonitor) || lastMonitor.monitorName !== f.monitorName) {
						lastMonitor = {
							monitorName: f.monitorName,
							monitorClass: f.monitorClass,
							children: []
						}

						lastAgent.children.push(lastMonitor);
					}

					lastMonitor.children.push(f);

					dst.FlatMonitorAssets.push(f);
				}
			} else {
				const idx = dst.TestData.push(f);

				if (f.type === FaultFileType.Input)
					f.displayName = "#" + idx.toString() + " - Rx - " + f.name;
				else
					f.displayName = "#" + idx.toString() + " - Tx - " + f.name;
			}
		}
	}

	export interface IFaultSummaryScope extends IViewModelScope {
		FaultSummaryTitle: string;
	}

	export class FaultsController {
		static $inject = [
			C.Angular.$scope,
			C.Angular.$state
		];

		constructor(
			$scope: IFaultSummaryScope,
			$state: ng.ui.IStateService
		) {
			$scope.FaultSummaryTitle = FaultsTitle($state.params['bucket']);
		}
	}
}
