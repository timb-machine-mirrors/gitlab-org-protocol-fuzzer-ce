 /// <reference path="../reference.ts" />

module Peach.Models {
	"use strict";

	export class IPit {
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
		pit: Pit;
	}

	export class Pit extends IPit {
		public get configured(): boolean {
			if (this.hasVersion === false) {
				this.addVersion();
			}

			return this.versions[this.versions.length - 1].configured;
		}

		public set configured(value: boolean) {
			if (this.hasVersion === false) {
				this.addVersion();
			}
			this.versions[this.versions.length - 1].configured = value;
		}

		constructor(pit?: IPit) {
			super();

			if (pit !== undefined) {
				this.pitUrl = pit.pitUrl;
				this.name = pit.name;
				this.description = pit.description;
				this.tags = pit.tags;
				this.locked = pit.locked;

				if (pit.versions === undefined) {
					this.addVersion();
				} else {
					this.versions = pit.versions;
				}
			}
		}

		private addVersion() {
			var v: IPitVersion = {
				locked: false,
				configured: false,
				version: 0
			};
			if (this.versions === undefined) {
				this.versions = [];
			}
			this.versions.push(v);
		}

		public get hasVersion(): boolean {
			return (this.versions !== undefined && this.versions.length > 0);
		}
	}
}
