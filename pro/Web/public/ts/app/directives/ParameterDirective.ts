/// <reference path="../reference.ts" />

module Peach {
	"use strict";

	export class ParameterDirective implements ng.IDirective {
		public restrict = 'E';
		public templateUrl = 'html/directives/parameter.html';
		public controller = ParameterController;
		public scope = { param: '=' };
	}

	export interface IParameterScope extends IFormScope {		
		param: Models.IParameter;
	}

	export class ParameterController {
		static $inject = [
			"$scope",
			"PitService"
		];

		constructor(
			private $scope: IParameterScope,
			private pitService: Services.PitService
		) {
			$scope.vm = this;
		}

		public get IsRequired(): boolean {
			return _.isUndefined(this.$scope.param.defaultValue);
		}

		public get Choices(): string[] {
			return this.defines().concat(this.$scope.param.options);
		}

		private defines(): string[] {
			var names = _.pluck(this.pitService.PitConfig.config, 'key');
			return _.map(names, x => '##' + x + '##');
		}
	}
}
