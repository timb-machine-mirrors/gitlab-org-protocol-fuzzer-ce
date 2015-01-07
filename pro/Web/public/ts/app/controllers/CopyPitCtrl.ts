/// <reference path="../reference.ts" />

module Peach {
	"use strict";

	export class CopyPitController {
		public Error: string = "";

		static $inject = [
			Constants.Angular.$scope,
			Constants.Angular.$modalInstance,
			Constants.Services.Pit,
			"pit"
		];

		constructor(
			$scope: IViewModelScope,
			private $modalInstance: ng.ui.bootstrap.IModalServiceInstance,
			private pitService: PitService,
			public Pit: IPit
		) {
			$scope.vm = this;
		}

		public Submit() {
			this.Error = "";

			var promise = this.pitService.CopyPit(this.Pit);
			promise.then((pit: IPit) => {
				this.$modalInstance.close(pit);
			}, (response: ng.IHttpPromiseCallbackArg<IPit>) => {
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
	}
}
