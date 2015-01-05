/// <reference path="../reference.ts" />

module Peach {
	"use strict";

	interface IBoundScope extends ng.IScope {
		min: IBoundFunction;
		max: IBoundFunction;
	}

	interface IBoundFunction {
		(): number;
	}

	interface IBoundPredicate {
		(value: number): boolean;
	}

	export class RangeDirective implements ng.IDirective {
		constructor(
		) {
			this.link = this._link.bind(this);
		}

		public restrict = 'A';
		public require = 'ngModel';
		public scope = {
			min: '&peachRangeMin',
			max: '&peachRangeMax'
		};
		public link;

		private _link(
			scope: IBoundScope,
			element: ng.IAugmentedJQuery,
			attrs: ng.IAttributes,
			ctrl: ng.INgModelController
		) {
			if (scope.min) {
				this.boundsValidate('rangeMin', ctrl,
					(value: number) => (value >= scope.min())
				);
			}
			if (scope.max) {
				this.boundsValidate('rangeMax', ctrl,
					(value: number) => (value <= scope.max())
				);
			}
		}

		private boundsValidate(
			name: string,
			ctrl: ng.INgModelController,
			predicate: IBoundPredicate
		) {
			var validator = value => {
				var isValid = (_.isEmpty(value) || predicate(value));
				ctrl.$setValidity(name, isValid);
				return value;
			};

			ctrl.$parsers.push(validator);
			ctrl.$formatters.push(validator);
		}
	}

	function regexValidate(
		name: string,
		pattern: RegExp,
		ctrl: ng.INgModelController
	) {
		var validator = value => {
			var isValid = (_.isEmpty(value) || pattern.test(value));
			ctrl.$setValidity(name, isValid);
			return value;
		};
		ctrl.$parsers.unshift(validator);
		ctrl.$formatters.unshift(validator);
	}

	export class IntegerDirective implements ng.IDirective {
		constructor(
		) {
			this.link = this._link.bind(this);
		}

		public restrict = 'A';
		public require = 'ngModel';
		public link;

		private _link(
			scope: ng.IScope,
			element: ng.IAugmentedJQuery,
			attrs: ng.IAttributes,
			ctrl: ng.INgModelController
		) {
			regexValidate('integer', /^\-?\d+$/, ctrl);
		}
	}

	export class HexDirective implements ng.IDirective {
		constructor(
		) {
			this.link = this._link.bind(this);
		}

		public restrict = 'A';
		public require = 'ngModel';
		public link;

		private _link(
			scope: ng.IScope,
			element: ng.IAugmentedJQuery,
			attrs: ng.IAttributes,
			ctrl: ng.INgModelController
		) {
			regexValidate('hexstring', /^[0-9A-Fa-f]+$/, ctrl);
		}
	}
}
