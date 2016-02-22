import { AWAIT_MARKER } from 'redux-await';

import { Library, LibraryState, Category } from '../../models/Library';
import { MakeEnum } from '../../utils';
import { api } from '../../services';

const types = {
	LIBRARY_FETCH: ''
};
MakeEnum(types);

const initial: LibraryState = {
	libraryUrl: null,
	pits: [],
	configurations: []
};

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

function mapLibrary(data: Library[]): Category[] {
	let ret: Category[] = [];
	for (const lib of data) {
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
	}
	return ret;
}

function onReceive(state: LibraryState, action): LibraryState {
	const library: Library[] = action.payload.library;
	const libraryUrl = _(library)
		.reject({ locked: true })
		.first()
		.libraryUrl;
	const pits = _.filter(library, 'locked');
	const configurations = _.reject(library, 'locked');
	return {
		libraryUrl: libraryUrl,
		pits: mapLibrary(pits),
		configurations: mapLibrary(configurations)
	};
}
