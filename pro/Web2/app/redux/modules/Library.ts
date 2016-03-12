import { AWAIT_MARKER } from 'redux-await';

import { Library, LibraryState, Category } from '../../models/Library';
import { MakeEnum } from '../../utils';
import { api } from '../../services';

const types = {
	LIBRARY_FETCH: ''
};
MakeEnum(types);

const initial: LibraryState = {};

export default function reducer(state: LibraryState = initial, action): LibraryState {
	switch (action.type) {
		case types.LIBRARY_FETCH:
			return onReceive(state, action);
		default:
			return state;
	}
}

export function fetchLibraries() {
	return {
		type: types.LIBRARY_FETCH,
		AWAIT_MARKER,
		payload: {
			library: api.fetchLibraries()
		}
	};
}

function mapLibrary(lib: Library): Category[] {
	let ret: Category[] = [];
	for (const version of lib.versions) {
		for (const pit of version.pits) {
			const name = _.find(pit.tags, tag =>
				_.startsWith(tag.name, 'Category')
			).values[1];

			let category = _.find(ret, { name: name });
			if (!category) {
				category = {
					name: name,
					pits: []
				};
				ret.push(category);
			}
			category.pits.push(pit);
		}
	}
	return ret;
}

interface DictIteratee<T> {
	(item: T): any[];
}

function dict<T>(collection: T[], iteratee: DictIteratee<T>) {
	const ret = {};
	for (const item of collection) {
		const pair = iteratee(item);
		ret[pair[0]] = pair[1];
	}
	return ret;
}

function onReceive(state: LibraryState, action): LibraryState {
	const library: Library[] = action.payload.library;
	return <LibraryState>dict(library, lib => [lib.name, mapLibrary(lib)]);
}
