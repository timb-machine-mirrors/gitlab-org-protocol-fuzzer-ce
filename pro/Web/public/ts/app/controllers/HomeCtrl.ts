/// <reference path="../reference.ts" />

namespace Peach {
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
