/// <reference path="../reference.ts" />

module Peach {
	"use strict";

	export class ConfigureVariablesController {

		static $inject = [
			Constants.Angular.$scope,
			Constants.Angular.$modal,
			Constants.Services.Pit
		];

		constructor(
			private $scope: IFormScope,
			private $modal: ng.ui.bootstrap.IModalService,
			private pitService: PitService
		) {
			$scope.vm = this;
			var promise = pitService.LoadPitConfig();
			promise.then((pitConfig: IPitConfig) => {
				this.PitConfig = pitConfig;
			});
		}

		private isSaved: boolean = false;

		public PitConfig: IPitConfig;

		public get ShowSaved() {
			return !this.$scope.form.$dirty && this.isSaved;
		}

		public get ShowValidation() {
			return this.$scope.form.$dirty && this.$scope.form.$invalid;
		}

		public get CanSave() {
			return this.$scope.form.$dirty && !this.$scope.form.$invalid;
		}

		public CanRemove(param: IParameter) {
			return param.type === 'user';
		}

		public OnSave(): void {
			this.PitConfig.$save({ id: this.pitService.PitId }, () => {
				this.isSaved = true;
				this.$scope.form.$setPristine();
			});
		}

		public OnAdd() {
			var modal = this.$modal.open({
				templateUrl: "html/modal/NewVar.html",
				controller: NewVarController
			});

			modal.result.then((param: IParameter) => {
				this.PitConfig.config.push(param);
				this.$scope.form.$setDirty();
			});
		}

		public OnRemove(index: number) {
			this.PitConfig.config.splice(index, 1);
			this.$scope.form.$setDirty();
		}
	}

	export class NewVarController {
		static $inject = [
			Constants.Angular.$scope,
			Constants.Angular.$modalInstance,
			Constants.Services.Pit
		];

		constructor(
			private $scope: IFormScope,
			private $modalInstance: ng.ui.bootstrap.IModalServiceInstance,
			private pitService: PitService
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

		public Param: IParameter;

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
