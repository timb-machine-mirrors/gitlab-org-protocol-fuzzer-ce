import { Dispatch } from 'redux';
import { AWAIT_MARKER } from 'redux-await';
import superagent = require('superagent');

import { Job } from '../../models/Job';
import { MakeEnum } from '../../utils';

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

export function fetch() {
	return (dispatch: Dispatch, getState: Function) => {
		dispatchFetch(dispatch);
	}
}

export function dispatchFetch(dispatch: Dispatch) {
	dispatch({
		type: types.JOBS_FETCH,
		AWAIT_MARKER,
		payload: {
			jobs: doFetch()
		}
	});
}

function doFetch() {
	return new Promise<Job[]>((resolve, reject) => {
		superagent.get('/p/jobs')
			.query({ dryrun: false })
			.accept('json')
			.end((err, res) => {
				if (err) {
					reject(`Job listing failed to load: ${err.message}`);
				} else {
					resolve(res.body);
				}
			})
		;
	});
}
