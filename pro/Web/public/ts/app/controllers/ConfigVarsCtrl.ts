/// <reference path="../reference.ts" />

module Peach {
	"use strict";

	export class ConfigureVariablesController {
		public PitConfig: Models.IPitConfig;

		static $inject = [
			"$scope",
			"$modal",
			"PitService"
		];

		constructor(
			private $scope: IFormScope,
			private $modal: ng.ui.bootstrap.IModalService,
			private pitService: Services.PitService
		) {
			$scope.vm = this;
			this.PitConfig = pitService.LoadPitConfig();
		}

		public get ShowSaved() {
			return !this.$scope.form.$dirty && !this.$scope.form.$pristine;
		}

		public get ShowValidation() {
			return this.$scope.form.$dirty && this.$scope.form.$invalid;
		}

		public get CanSave() {
			return this.$scope.form.$dirty && !this.$scope.form.$invalid;
		}

		public CanRemove(param: Models.IParameter) {
			return param.type === 'user';
		}

		public IsReadonly(param: Models.IParameter) {
			return param.type === 'system';
		}

		public ParamTooltip(param: Models.IParameter) {
			return this.IsReadonly(param) ? param.value : '';
		}

		public OnSave(): void {
			this.PitConfig.$save({ id: this.pitService.PitId }, () => {
				this.$scope.form.$dirty = false;
			});
		}

		public OnAdd() {
			var modal = this.$modal.open({
				templateUrl: "html/modal/NewVar.html",
				controller: NewVarController
			});

			modal.result.then((param: Models.IParameter) => {
				this.PitConfig.config.push(param);
				this.$scope.form.$setDirty();
			});
		}

		public OnRemove(index: number) {
			this.PitConfig.config.splice(index, 1);
			this.$scope.form.$setDirty();
		}

		public Choices(param: Models.IParameter) {
			return this.defines().concat(param.options);
		}

		private defines(): string[]{
			var names = _.pluck(this.pitService.PitConfig.config, 'key');
			return _.map(names, x => '##' + x + '##');
		}
	}

	export class NewVarController {
		static $inject = [
			"$scope",
			"$modalInstance",
			"PitService"
		];

		constructor(
			private $scope: IFormScope,
			private $modalInstance: ng.ui.bootstrap.IModalServiceInstance,
			private pitService: Services.PitService
		) {
			$scope.vm = this;

			this.Param = {
				key: "",
				value: "",
				name: "",
				description: 'User-defined variable',
				type: 'user'
			};
		}

		private hasBlurred: boolean;

		public Param: Models.IParameter;

		public get ParamKeys(): string[] {
			return _.pluck(this.pitService.PitConfig.config, 'key');
		}
		
		public Cancel() {
			this.$modalInstance.dismiss();
		}

		public Accept() {
			this.$modalInstance.close(this.Param);
		}

		public OnNameBlur() {
			this.hasBlurred = true;
		}

		public OnNameChanged() {
			var value = this.Param.name;
			if (!this.hasBlurred) {
				if (_.isString(value)) {
					this.Param.key = value.replace(new RegExp(' ', 'g'), '');
				} else {
					this.Param.key = undefined;
				}
			}
		}
	}
}
