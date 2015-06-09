/// <reference path="../reference.ts" />

module Peach {
	"use strict";

	export class HomeController {
		static $inject = [
			C.Angular.$scope
		];

		constructor(
			$scope: IViewModelScope
		) {
		}
	}
}
