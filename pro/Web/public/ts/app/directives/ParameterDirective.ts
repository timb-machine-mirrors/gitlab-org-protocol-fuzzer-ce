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
		param: IParameter;
	}

	export interface IOption {
		key: string;
		description: string;
		isDefault: boolean;
	}

	export class ParameterController {
		static $inject = [
			"$scope",
			"PitService"
		];

		constructor(
			private $scope: IParameterScope,
			private pitService: PitService
		) {
			$scope.vm = this;
			this.makeChoices();
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

		public Choices: IOption[];

		public EnumGroups(item: IOption): string {
			if (item.isDefault) {
				return 'Default';
			}
			if (item.key.startsWith('##')) {
				return 'Defines';
			}
			return 'Choices';
		}

		public makeChoices() {
			var options = this.$scope.param.options || [];
			var tuples = [];

			options.forEach(item => {
				var option: IOption = {
					key: item,
					description: '',
					isDefault: item === this.$scope.param.defaultValue
				};

				if (option.isDefault) {
					tuples.unshift(option);
				} else {
					tuples.push(option);
				}
			});
			this.Choices = tuples.concat(this.defines());
		}

		private defines(): IOption[] {
			return _.map(this.pitService.PitConfig.config, param => {
				return <IOption> {
					key: '##' + param.key + '##',
					description: param.description,
					isDefault: false
				};
			});
		}
	}
}
