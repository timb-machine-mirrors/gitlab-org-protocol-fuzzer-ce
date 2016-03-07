/// <reference path="../reference.ts" />

namespace Peach {
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
			C.Angular.$uibModal,
			C.Services.Pit
		];

		constructor(
			$scope: IViewModelScope,
			private $state: ng.ui.IStateService,
			private $modal: ng.ui.bootstrap.IModalService,
			private pitService: PitService
		) {
			this.init();
			$scope['filterCategory'] = this.filterCategory;
		}

		private Pits: PitLibrary;
		private User: PitLibrary;
		
		private init() {
			const promise = this.pitService.LoadLibrary();
			promise.then((data: ILibrary[]) => {
				data.forEach((lib: ILibrary) => {
					const pitLib = new PitLibrary(lib.name);
					if (lib.locked) {
						this.Pits = pitLib;
					} else {
						this.User = pitLib;
					}
					
					lib.versions.forEach((version: ILibraryVersion) => {
						version.pits.forEach((pit: IPit) => {
							const category = _.find(pit.tags, (tag: ITag) =>
								tag.name.startsWith("Category")
							).values[1];
							
							let pitCategory = _.find(pitLib.Categories, { 'Name': category });
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

		private OnSelectPit(entry: PitEntry) {
			if (entry.Library.locked) {
				this.$modal.open({
					templateUrl: C.Templates.Modal.NewConfig,
					controller: NewConfigController,
					resolve: { Pit: () => angular.copy(entry.Pit) }
				}).result.then((copied: IPit) => {
					this.GoToPit(copied);
				});
			} else {
				this.GoToPit(entry.Pit);
			}
		}

		private GoToPit(pit: IPit) {
			this.$state.go(C.States.Pit, { pit: pit.id });
		}

		private filterCategory(search: string) {
			return function(category: PitCategory) {
				if (_.isEmpty(search)) {
					return true;
				}
				return _.some(category.Pits, entry => {
					return _.includes(entry.Pit.name.toLowerCase(), search.toLowerCase());
				});
			}
		}
	}
}
