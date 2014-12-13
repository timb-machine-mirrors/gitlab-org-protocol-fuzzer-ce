/// <reference path="../reference.ts" />

module Peach {
	"use strict";

	export class CopyPitController {
		public Error: string = "";

		static $inject = [
			"$scope",
			"$modalInstance",
			"PitService",
			"pit"
		];

		constructor(
			$scope: IViewModelScope,
			private $modalInstance: ng.ui.bootstrap.IModalServiceInstance,
			private pitService: Services.PitService,
			public Pit: Models.IPit
		) {
			$scope.vm = this;
		}

		public Submit() {
			this.Error = "";

			var promise = this.pitService.CopyPit(this.Pit);
			promise.then((pit: Models.IPit) => {
				this.$modalInstance.close(pit);
			}, (response: ng.IHttpPromiseCallbackArg<Models.IPit>) => {
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
