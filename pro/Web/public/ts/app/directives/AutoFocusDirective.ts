/// <reference path="../reference.ts" />

namespace Peach {
	"use strict";
	
	export var AutoFocusDirective: IDirective = {
		ComponentID: C.Directives.AutoFocus,
		restrict: 'AC',
		link: (scope: ng.IScope, element: ng.IAugmentedJQuery) => {
			_.delay(() => {
				element[0].focus();
			}, 10);
		}
	}
}
