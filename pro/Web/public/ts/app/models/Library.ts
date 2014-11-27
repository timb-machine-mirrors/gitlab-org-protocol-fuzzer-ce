/// <reference path="../reference.ts" />

module Peach.Models {
	"use strict";

	export interface ITag {
		name: string;
		values: string[];
	}

	export interface IPitLibrary extends ng.resource.IResource<IPitLibrary> {
		libraryUrl: string;
		name: string;
		description: string;
		locked: boolean;
		versions: IPitLibraryVersion[];
		groups: IGroup[];
		user: string;
		timeStamp: Date;
	}

	export interface IPitLibraryResource extends ng.resource.IResourceClass<IPitLibrary> {}

	export interface IPitLibraryVersion {
		version: number;
		locked: boolean;
		pits: Pit[];
		//user: string;
		//timeStamp: Date;
	}

	export interface IGroup {
		groupUrl: string;
		access: string;
	}

	export interface IPostMonitorsRequest {
		pitUrl: string;
		monitors: Agent[];
	}
}
