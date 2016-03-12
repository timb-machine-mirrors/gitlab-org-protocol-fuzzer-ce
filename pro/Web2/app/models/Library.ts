import { Pit } from './Pit';

export interface LibraryState {
	[name: string]: Category[];
}

export interface Library {
	libraryUrl: string;
	name: string;
	description: string;
	locked: boolean;
	versions: LibraryVersion[];
	groups: Group[];
	user: string;
	timeStamp: Date;
}

export interface LibraryVersion {
	version: number;
	locked: boolean;
	pits: Pit[];
}

export interface Group {
	groupUrl: string;
	access: string;
}

export interface Category {
	name: string;
	pits: Pit[];
}
