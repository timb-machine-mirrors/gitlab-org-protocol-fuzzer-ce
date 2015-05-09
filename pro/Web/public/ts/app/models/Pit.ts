 /// <reference path="../reference.ts" />

module Peach {
	"use strict";

	export interface IPit {
		id: string;
		pitUrl: string;
		name: string;
		description: string;
		tags: ITag[];
		locked: boolean;
		versions: IPitVersion[];

		// details, not available from collection at /p/pits
		peachConfig: IKeyValue[];
		config: IParameter[];
		agents: Agent[];
		calls: string[];
	}

	export interface IPitVersion {
		version: number;
		configured: boolean;
		locked: boolean;
	}

	export interface IPitCopy {
		// Url of the destination Pit Library
		libraryUrl: string;
		pitUrl: string;
		name: string;
		description: string;
	}
}
