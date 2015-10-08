/// <reference path="../reference.ts" />

module Peach {
	"use strict";

	export class ConfigureVariablesController {
		static $inject = [
			C.Angular.$scope,
			C.Angular.$modal,
			C.Services.Pit
		];

		constructor(
			private $scope: IFormScope,
			private $modal: ng.ui.bootstrap.IModalService,
			private pitService: PitService
		) {
			var promise = pitService.LoadPit();
			promise.then((pit: IPit) => {
				this.Config = pit.config;
				this.hasLoaded = true;
			});
		}

		private hasLoaded: boolean = false;
		private isSaved: boolean = false;
		public Config: IParameter[];

		public get ShowLoading(): boolean {
			return !this.hasLoaded;
		}

		public get ShowSaved() {
			return !this.$scope.form.$dirty && this.isSaved;
		}

		public get ShowRequired() {
			return this.$scope.form.$pristine && this.$scope.form.$invalid;
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
			var promise = this.pitService.SaveVars(this.Config);
			promise.then(() => {
				this.isSaved = true;
				this.$scope.form.$setPristine();
			});
		}

		public OnAdd() {
			var modal = this.$modal.open({
				templateUrl: C.Templates.Modal.NewVar,
				controller: NewVarController
			});

			modal.result.then((param: IParameter) => {
				this.Config.push(param);
				this.$scope.form.$setDirty();
			});
		}

		public OnRemove(index: number) {
			this.Config.splice(index, 1);
			this.$scope.form.$setDirty();
		}
	}
}
