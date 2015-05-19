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
		
		public Pits: IPit[] = [];
	}
	
	export class LibraryController {
		static $inject = [
			C.Angular.$scope,
			C.Services.Pit
		];

		constructor(
			$scope: IViewModelScope,
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
							
							pitCategory.Pits.push(pit);
						});
					});
				});
			});
		}
	}
}
