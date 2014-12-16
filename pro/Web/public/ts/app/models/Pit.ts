 /// <reference path="../reference.ts" />

module Peach {
	"use strict";

	import IResource = ng.resource.IResource;
	import IResourceClass = ng.resource.IResourceClass;

	export interface IPit extends IResource<IPit> {
		pitUrl: string;
		name: string;
		description: string;
		tags: ITag[];
		locked: boolean;
		versions: IPitVersion[];
	}

	export interface IPitVersion {
		version: number;
		configured: boolean;
		locked: boolean;
	}

	export interface IPitCopy {
		// Url of the destination Pit Library
		libraryUrl: string;

		// Pit record. Use only pitUrl, name, and description
		pit: IPit;
	}

	// resources
	export interface IPitResource extends IResourceClass<IPit> { }
}
