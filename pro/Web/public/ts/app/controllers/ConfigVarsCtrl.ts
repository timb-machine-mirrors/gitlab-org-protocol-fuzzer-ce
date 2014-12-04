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

		private grid: ngGrid.IGridOptions = {
			data: "vm.PitConfig.config",
			columnDefs: [
				{ field: "name", displayName: "Name", enableCellEdit: false },
				{ field: "key", displayName: "Key", enableCellEdit: false },
				{
					field: "value",
					displayName: "Value",
					editableCellTemplate: "html/grid/vars/edit-value.html"
				},
				{
					width: "27px",
					enableCellEdit: false,
					cellTemplate: "html/grid/vars/actions.html"
				}
			],
			enableRowSelection: false,
			multiSelect: false,
			enableCellEditOnFocus: true,
			plugins: [new ngGridFlexibleHeightPlugin()]
		};

		public get ShowSaved() {
			return !this.$scope.form.$dirty && !this.$scope.form.$pristine;
		}

		public get ShowValidation() {
			return this.$scope.form.$invalid && this.$scope.form.$dirty;
		}

		public get CanSave() {
			return this.$scope.form.$dirty;
		}

		public Save(): void {
			this.PitConfig.$save({ id: this.pitService.PitId }, () => {
				this.$scope.form.$dirty = false;
			});
		}

		public ShowActions(row: ngGrid.IRow) {
			var param = <Models.IParameter> row.entity;
			return param.type === 'user';
		}

		public Add() {
			var modal = this.$modal.open({
				templateUrl: "html/modal/NewVar.html",
				controller: NewVarController
			});

			modal.result.then((param: Models.IParameter) => {
				this.PitConfig.config.push(param);
				this.$scope.form.$setDirty();
			});
		}

		public RemoveRow(row: ngGrid.IRow) {
			this.PitConfig.config.splice(row.rowIndex, 1);
			this.$scope.form.$setDirty();
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
