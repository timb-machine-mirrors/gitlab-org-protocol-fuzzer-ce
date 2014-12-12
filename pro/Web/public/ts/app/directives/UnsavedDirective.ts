/// <reference path="../reference.ts" />

module Peach {
	"use strict";

	export class UnsavedDirective implements ng.IDirective {
		constructor(
			private $modal: ng.ui.bootstrap.IModalService,
			private $location: ng.ILocationService
		) {
			this.link = this._link.bind(this);
		}

		public restrict = 'A';
		public require = '^form';
		public scope = {};

		public link;

		public _link(
			scope: ng.IScope,
			element: ng.IAugmentedJQuery,
			attrs: ng.IAttributes,
			form: ng.IFormController
		) {
			var onRouteChangeOff = scope.$root.$on('$locationChangeStart', (
				event: ng.IAngularEvent,
				newUrl: string
			) => {

				if (!form.$dirty) {
					onRouteChangeOff();
					return;
				}

				var modal = this.$modal.open({
					templateUrl: "html/modal/Unsaved.html",
					controller: UnsavedModalController
				});
				modal.result.then((result) => {
					if (result === 'ok') {
						onRouteChangeOff();

						var path = newUrl.substr(newUrl.indexOf('#') + 1);
						this.$location.path(path);
					}
				});

				event.preventDefault();
			});
		}
	}

	class UnsavedModalController {

		static $inject = [
			"$scope",
			"$modalInstance"
		];

		constructor(
			private $scope: IFormScope,
			private $modalInstance: ng.ui.bootstrap.IModalServiceInstance
		) {
			$scope.vm = this;
		}

		public OnCancel() {
			this.$modalInstance.dismiss();
		}

		public OnSubmit() {
			this.$modalInstance.close('ok');
		}
	}
}
