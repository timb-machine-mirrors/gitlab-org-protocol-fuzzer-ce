/// <reference path="../reference.ts" />

module Peach {
	"use strict";

	class PitLibrary {
		constructor(
			public Name: string
		) {}

		public Categories: PitCategory[] = [];
	}
	
	class PitCategory {
		constructor(
			public Name: string
		) {}
		
		public Pits: PitEntry[] = [];
	}

	class PitEntry {
		constructor(
			public Library: ILibrary,
			public Pit: IPit
		) {}
	}
	
	export class LibraryController {
		static $inject = [
			C.Angular.$scope,
			C.Angular.$state,
			C.Angular.$modal,
			C.Services.Pit
		];

		constructor(
			$scope: IViewModelScope,
			private $state: ng.ui.IStateService,
			private $modal: ng.ui.bootstrap.IModalService,
			private pitService: PitService
		) {
			this.init();
		}

		public Libraries: PitLibrary[] = [];
		
		private init() {
			var promise = this.pitService.LoadLibrary();
			promise.then((data: ILibrary[]) => {
				data.forEach((lib: ILibrary) => {
					var pitLib = new PitLibrary(lib.name);
					this.Libraries.push(pitLib);
					
					lib.versions.forEach((version: ILibraryVersion) => {
						version.pits.forEach((pit: IPit) => {
							var category = _.find(pit.tags, (tag: ITag) =>
								tag.name.startsWith("Category")
							).values[1];
							
							var pitCategory = _.find(pitLib.Categories, { 'Name': category });
							if (!pitCategory) {
								pitCategory = new PitCategory(category);
								pitLib.Categories.push(pitCategory);
							}
							
							pitCategory.Pits.push(new PitEntry(lib, pit));
						});
					});
				});
			});
		}

		public OnSelectPit(entry: PitEntry) {
			if (entry.Library.locked) {
				this.$modal.open({
					templateUrl: C.Templates.Modal.CopyPit,
					controller: CopyPitController,
					resolve: { Pit: () => entry.Pit }
				}).result.then((copied: IPit) => {
					this.GoToPit(copied);
				});
			} else {
				this.GoToPit(entry.Pit);
			}
		}

		private GoToPit(pit: IPit) {
			this.$state.go(C.States.PitConfigure, { pit: pit.id });
		}
	}
}
