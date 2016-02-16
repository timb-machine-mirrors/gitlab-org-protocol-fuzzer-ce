import { AWAIT_MARKER } from 'redux-await';

import { Job } from '../../models/Job';
import { MakeEnum } from '../../utils';
import { api } from '../../services';

const types = {
	JOBS_FETCH: ''
};
MakeEnum(types);

export default function reducer(state: Job[] = [], action): Job[] {
	switch (action.type) {
		case types.JOBS_FETCH:
			return action.payload.jobs;
		default:
			return state;
	}
}

export function fetchJobs() {
	return {
		type: types.JOBS_FETCH,
		AWAIT_MARKER,
		payload: {
			jobs: api.fetchJobs()
		}
	};
}
