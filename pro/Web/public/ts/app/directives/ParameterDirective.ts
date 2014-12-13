/// <reference path="../reference.ts" />

module Peach {
	"use strict";

	export class ParameterDirective implements ng.IDirective {
		public restrict = 'E';
		public templateUrl = 'html/directives/parameter.html';
		public controller = ParameterController;
		public scope = { param: '=' };
	}

	export class ParameterInputDirective implements ng.IDirective {
		public restrict = 'E';
		public templateUrl = 'html/directives/parameter-input.html';
		public controller = ParameterController;
		public scope = {
			param: '=',
			form: '='
		};
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

		public get IsReadonly() {
			return this.$scope.param.type === 'system';
		}

		public get ParamTooltip() {
			return this.IsReadonly ? this.$scope.param.value : '';
		}

		public get UseSelect(): boolean {
			return this.$scope.param.type === 'enum' ||
				this.$scope.param.type === 'bool';
		}

		public get Choices(): string[] {
			var options = this.$scope.param.options || [];
			return options.concat(this.defines());
		}

		public EnumGroups(item: string): string {
			if (item.startsWith('##')) {
				return 'Defines';
			}
			return 'Choices';
		}

		private defines(): string[] {
			var names = _.pluck(this.pitService.PitConfig.config, 'key');
			return _.map(names, x => '##' + x + '##');
		}
	}
}
