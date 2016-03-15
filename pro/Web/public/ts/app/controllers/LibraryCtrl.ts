/// <reference path="../reference.ts" />

namespace Peach {
	const LibText = {
		Pits: 'Peach Pits allow testing of a data format or a network protocol against a variety of targets.',
		Configurations: 'The Configurations section contains existing Peach Pit configurations. Selecting an existing configuration allows editing the configuration and starting a new fuzzing job.',
		Legacy: ''
	};
	
	export class PitLibrary {
		constructor(
			public Name: string
		) {
			this.Text = LibText[Name];
		}

		public Text: string;
		public Categories: PitCategory[] = [];
	}

	export class PitCategory {
		constructor(
			public Name: string
		) {}
		
		public Pits: PitEntry[] = [];
	}

	export class PitEntry {
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
		}

		private Libs: PitLibrary[] = [];
		
		private init() {
			const promise = this.pitService.LoadLibrary();
			promise.then((data: ILibrary[]) => {
				for (const lib of data) {
					const pitLib = new PitLibrary(lib.name);
					let hasPits = false;
					
					for (const version of lib.versions) {
						for (const pit of version.pits) {
							const category = _.find(pit.tags, (tag: ITag) =>
								tag.name.startsWith("Category")
							).values[1];
							
							let pitCategory = _.find(pitLib.Categories, { 'Name': category });
							if (!pitCategory) {
								pitCategory = new PitCategory(category);
								pitLib.Categories.push(pitCategory);
							}
							
							pitCategory.Pits.push(new PitEntry(lib, pit));
							hasPits = true;
						};
					};

					if (pitLib.Name !== 'Legacy' || hasPits) {
						this.Libs.push(pitLib);
					}
				}
			});
		}

		private OnSelectPit(entry: PitEntry) {
			if (entry.Library.versions[0].version === 1) {
				this.$modal.open({
					templateUrl: C.Templates.Modal.MigratePit,
					controller: MigratePitController,
					resolve: {
						Lib: () => _.find(this.Libs, { Name: 'Pits' }),
						Pit: () => angular.copy(entry.Pit)
					}
				}).result.then((newPit: IPit) => {
					this.GoToPit(newPit);
				});
			} else if (entry.Library.locked) {
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
			return (category: PitCategory) => {
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
