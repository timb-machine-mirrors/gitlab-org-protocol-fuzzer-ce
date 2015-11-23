/// <reference path="../reference.ts" />

namespace Peach {
	"use strict";

	export class NewConfigController {
		public Error: string = "";

		static $inject = [
			C.Angular.$scope,
			C.Angular.$modalInstance,
			C.Services.Pit,
			"Pit"
		];

		constructor(
			$scope: IViewModelScope,
			private $modalInstance: ng.ui.bootstrap.IModalServiceInstance,
			private pitService: PitService,
			public Pit: IPit
		) {
			$scope.vm = this;
		}

		private pending: boolean = false;

		public Submit() {
			this.Error = "";

			this.pending = true;
			this.pitService.SaveConfig(this.Pit)
				.then((response: ng.IHttpPromiseCallbackArg<IPit>) => {
					this.pending = false;
					this.$modalInstance.close(response.data);
				},
				(response: ng.IHttpPromiseCallbackArg<any>) => {
					this.pending = false;
					switch (response.status) {
						case 400:
							this.Error = this.Pit.name + " already exists, please choose a new name.";
							break;
						default:
							this.Error = "Error: " + response.statusText;
							break;
					}
				});
		}

		public Cancel() {
			this.$modalInstance.dismiss();
		}

		public get IsSubmitDisabled(): boolean {
			return this.pending;
		}
	}
}
