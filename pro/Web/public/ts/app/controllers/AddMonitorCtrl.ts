/// <reference path="../reference.ts" />

namespace Peach {
	"use strict";

	export class AddMonitorController {
		static $inject = [
			C.Angular.$scope,
			C.Angular.$uibModalInstance,
			C.Services.Pit
		];

		constructor(
			$scope: IViewModelScope,
			private $modalInstance: ng.ui.bootstrap.IModalServiceInstance,
			private pitService: PitService
		) {
			$scope.vm = this;
			pitService.LoadPeachMonitors().then((monitors: IMonitor[]) => {
				this.PeachMonitors = monitors;
			});
		}

		public PeachMonitors: IMonitor[];

		public get CanAccept(): boolean {
			return false;
		}
		
		public Accept() {
		}

		public Cancel() {
			this.$modalInstance.dismiss();
		}

		public Groups = [
			{
				Name: "Power Control",
				Items: [
					{
						Name: "Gdb",
						Description: "Uses GDB to launch an executable, monitoring it for exceptions"
					},
					{
						Name: "Randofaulter",
						Description: "Generate random faults for metrics testing"
					}
				]
			},
			{
				Name: "Things",
				Items: [
					{
						Name: "Gdb",
						Description: "Uses GDB to launch an executable, monitoring it for exceptions"
					},
					{
						Name: "Randofaulter",
						Description: "Generate random faults for metrics testing"
					},
					{
						Name: "Gdb",
						Description: "Uses GDB to launch an executable, monitoring it for exceptions"
					},
					{
						Name: "Randofaulter",
						Description: "Generate random faults for metrics testing"
					}
				]
			},
			{
				Name: "Other Stuff",
				Items: [
					{
						Name: "Gdb",
						Description: "Uses GDB to launch an executable, monitoring it for exceptions"
					},
					{
						Name: "Randofaulter",
						Description: "Generate random faults for metrics testing"
					},
					{
						Name: "Gdb",
						Description: "Uses GDB to launch an executable, monitoring it for exceptions"
					},
					{
						Name: "Randofaulter",
						Description: "Generate random faults for metrics testing"
					}
				]
			}
		];
	}
}
