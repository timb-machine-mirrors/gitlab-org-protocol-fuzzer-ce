 /// <reference path="../reference.ts" />

module Peach {
	"use strict";

	export interface IPit {
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

		// Pit record. Use only pitUrl, name, and description
		pit: IPit;
	}
}
