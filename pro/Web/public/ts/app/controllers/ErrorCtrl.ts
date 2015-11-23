/// <reference path="../reference.ts" />

namespace Peach {
	"use strict";

	export class ErrorController {
		static $inject = [
			C.Angular.$scope,
			C.Angular.$state
		];

		constructor(
			$scope: IViewModelScope,
			$state: ng.ui.IStateService
		) {
			this.Message = $state.params['message'] || 'An unknown error has occured.';
		}

		public Message: string;
	}
}
