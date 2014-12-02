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
					field: "_actions",
					displayName: "",
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
			});
		}

		public RemoveRow(row: ngGrid.IRow) {
			this.PitConfig.config.splice(row.rowIndex, 1);
		}
	}

	export class NewVarController {
		public Param: Models.IParameter;

		static $inject = [
			"$scope",
			"$modalInstance"
		];

		constructor(
			$scope: IViewModelScope,
			private $modalInstance: ng.ui.bootstrap.IModalServiceInstance
		) {
			$scope.vm = this;

			this.Param = {
				key: null,
				value: null,
				name: null,
				description: 'User-defined variable',
				type: 'user',
				enumType: undefined,
				defaults: [],
				min: undefined,
				max: undefined
			};
		}

		public Cancel() {
			this.$modalInstance.dismiss();
		}

		public Accept() {
			this.$modalInstance.close(this.Param);
		}
	}
}
