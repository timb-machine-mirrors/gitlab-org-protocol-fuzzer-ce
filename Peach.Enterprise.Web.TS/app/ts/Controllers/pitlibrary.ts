/// <reference path="../Models/wizard.ts" />
/// <reference path="../Models/peach.ts" />
/// <reference path="../Models/peach.ts" />
/// <reference path="../../../Scripts/typings/angularjs/angular.d.ts" />
/// <reference path="../../../scripts/typings/kendo/kendo.web.d.ts" />

module DashApp {

	import P = Models.Peach;

	export class PitLibraryController {
		_libraries: any;

		get libraries(): any {
			return this._libraries;
		}

		selectedPit: string;

		treeOptions = {
			nodeChildren: "children",
			dirSelectable: true
		}

		private modalInstance: ng.ui.bootstrap.IModalServiceInstance;

		constructor($scope: ViewModelScope, $modalInstance: ng.ui.bootstrap.IModalServiceInstance, peachsvc: Services.IPeachService) {
			$scope.vm = this;
			this.modalInstance = $modalInstance;

			peachsvc.GetLibraries((data: P.PitLibrary[]) => {
				if (data != undefined && data.length > 0) {
					this._libraries = new kendo.data.HierarchicalDataSource({
						data: TreeItem.CreateFromPitLibrary(data)
					});
				}
			});
		}

		selectPit() {
			this.modalInstance.close(this.selectedPit);
		}
	}

	export class TreeItem {
		text: string;
		pitUrl: string;
		items: TreeItem[];

		static CreateTestData(): TreeItem[] {
			var output: TreeItem[] = [];

			for (var i = 0; i < 3; i++) {
				var item: TreeItem = new TreeItem();
				item.text = "treeitem" + i.toString();
			}

			return output;
		}

		static CreateFromPitLibrary(pitLibrary: P.PitLibrary[]): TreeItem[] {
			var output: TreeItem[] = [];

			for (var l = 0; l < pitLibrary.length; l++) {
				var libitem: TreeItem = new TreeItem();
				libitem.text = pitLibrary[l].name;
				libitem.items = [];
				for (var v = 0; v < pitLibrary[l].versions.length; v++) {
					for (var p = 0; p < pitLibrary[l].versions[v].pits.length; p++) {
						var pititem: TreeItem = new TreeItem();
						pititem.text = pitLibrary[l].versions[v].pits[p].name;
						pititem.pitUrl = pitLibrary[l].versions[v].pits[p].pitUrl;
						libitem.items.push(pititem);
					}
				}
				output.push(libitem);
			}

			return output;
		}
	}
}