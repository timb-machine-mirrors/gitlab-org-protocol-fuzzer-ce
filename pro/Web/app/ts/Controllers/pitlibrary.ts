/// <reference path="../../../Scripts/typings/angular-ui-bootstrap/angular-ui-bootstrap.d.ts" />
/// <reference path="../Services/peach.ts" />
/// <reference path="main.ts" />

module DashApp {
	"use strict";

	export class PitLibraryController {
		private _libraries: any;

		public get libraries(): any {
			return this._libraries;
		}

		public selectedPit: string;

		public treeOptions = {
			nodeChildren: "items",
			dirSelectable: false
		}

		public notAPit:boolean = false;
		
		private modalInstance: ng.ui.bootstrap.IModalServiceInstance;

		constructor($scope: ViewModelScope, $modalInstance: ng.ui.bootstrap.IModalServiceInstance, peachsvc: Services.IPeachService, canCancel: boolean) {
			$scope.vm = this;
			this.modalInstance = $modalInstance;
			this.CanCancel = canCancel;

			peachsvc.GetLibraries((data: Models.PitLibrary[]) => {
				if (data != undefined && data.length > 0) {
					this._libraries = TreeItem.CreateFromPitLibrary(data);
				}
			});
		}

		public CanCancel: boolean;

		public changeSelection(pitUrl: string) {
			this.notAPit = pitUrl == undefined;
			if (pitUrl != undefined)
				this.selectedPit = pitUrl;
		}

		public selectPit() {
			if (this.selectedPit == undefined) {
				this.notAPit = true;
			} else {
				this.modalInstance.close(this.selectedPit);
			}
		}

		public exportPit() {

		}

		public cancel() {
			if (this.CanCancel) {
				this.modalInstance.dismiss();
			}
		}
	}

	export class TreeItem {
		id: number;
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

		static CreateFromPitLibrary(pitLibrary: Models.PitLibrary[]): TreeItem[] {
			var output: TreeItem[] = [];

			var libitem: TreeItem;
			var catitem: TreeItem;
			var pititem: TreeItem;
			var id = 0;

			for (var l = 0; l < pitLibrary.length; l++) {
				if (pitLibrary[l].versions[0].pits.length > 0) {
					libitem = new TreeItem();
					libitem.id = id++;
					libitem.text = pitLibrary[l].name;
					libitem.items = [];
					for (var v = 0; v < pitLibrary[l].versions.length; v++) {
						for (var p = 0; p < pitLibrary[l].versions[v].pits.length; p++) {
							var category = $.grep(pitLibrary[l].versions[v].pits[p].tags, (e) => {
								return e.name.substr(0, 8) == "Category";
							})[0].values[1];

							catitem = $.grep(libitem.items, (e) => { return e.text == category; })[0];
							if (catitem == undefined) {
								catitem = new TreeItem();
								catitem.id = id++;
								catitem.text = category;
								catitem.items = [];
								libitem.items.push(catitem);
							}

							pititem = new TreeItem();
							pititem.id = id++;
							pititem.text = pitLibrary[l].versions[v].pits[p].name;
							pititem.pitUrl = pitLibrary[l].versions[v].pits[p].pitUrl;
							catitem.items.push(pititem);
						}
					}
					output.push(libitem);
				}
			}

			return output;
		}
	}
}
