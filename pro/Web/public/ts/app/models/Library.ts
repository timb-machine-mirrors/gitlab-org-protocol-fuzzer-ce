/// <reference path="../reference.ts" />

module Peach.Models {
	"use strict";

	import IResourceClass = ng.resource.IResourceClass;
	import IResource = ng.resource.IResource;

	export interface ILibrary extends IResource<ILibrary> {
		libraryUrl: string;
		name: string;
		description: string;
		locked: boolean;
		versions: ILibraryVersion[];
		groups: IGroup[];
		user: string;
		timeStamp: Date;
	}

	export interface ILibraryVersion {
		version: number;
		locked: boolean;
		pits: IPit[];
	}

	export interface IGroup {
		groupUrl: string;
		access: string;
	}

	export interface ITag {
		name: string;
		values: string[];
	}

	// resources
	export interface ILibraryResource extends IResourceClass<ILibrary> { }
}
