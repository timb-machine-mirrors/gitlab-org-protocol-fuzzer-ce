/// <reference path="../reference.ts" />

namespace Peach {
	export interface IAddMonitorScope extends IViewModelScope {
		search: string;
	}

	export class AddMonitorController {
		static $inject = [
			C.Angular.$scope,
			C.Angular.$uibModalInstance,
			C.Services.Pit
		];

		private selected: IParameter;

		constructor(
			private $scope: IAddMonitorScope,
			private $modalInstance: ng.ui.bootstrap.IModalServiceInstance,
			private pitService: PitService
		) {
			$scope.vm = this;
		}

		public get Monitors(): IParameter[] {
			if (this.$scope.search) {
				let search = this.$scope.search.toLowerCase();
				let monitors: IParameter[] = [];
				for (let group of this.pitService.Pit.metadata.monitors) {
					if (_.any(group.items, (item: IParameter) => {
						let name = item.name.toLowerCase();
						let pos = name.indexOf(search);
						return pos != -1;
					})) {
						monitors.push(group);
					}
				}
				return monitors;
			}
			return this.pitService.Pit.metadata.monitors;
		}
		
		public get CanAccept(): boolean {
			return !_.isUndefined(this.selected);
		}

		public OnSelect(item: IParameter): void {
			this.selected = item;
		}
		
		public Accept(): void {
			this.$modalInstance.close(this.selected);
		}

		public Cancel(): void {
			this.$modalInstance.dismiss();
		}
	}
}
