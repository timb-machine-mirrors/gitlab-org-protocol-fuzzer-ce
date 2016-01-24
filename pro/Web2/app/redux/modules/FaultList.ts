import { Dispatch } from 'redux';
import { AWAIT_MARKER } from 'redux-await';
import superagent = require('superagent');

import { Job } from '../../models/Job';
import { FaultSummary } from '../../models/Fault';
import { MakeEnum } from '../../utils';

const types = {
	FAULTS_FETCH: ''
};
MakeEnum(types);

export default function reducer(state: FaultSummary[] = [], action): FaultSummary[] {
	switch (action.type) {
		case types.FAULTS_FETCH:
			return action.payload.faults;
		default:
			return state;
	}
}

export function fetch(job: Job) {
	return (dispatch: Dispatch, getState: Function) => {
		dispatch({
			type: types.FAULTS_FETCH,
			AWAIT_MARKER,
			payload: {
				faults: doFetch(job)
			}
		});
	};
}

export async function doFetch(job: Job) {
	return new Promise<FaultSummary[]>((resolve, reject) => {
		superagent.get(job.faultsUrl)
			.accept('json')
			.end((err, res) => {
				if (err) {
					reject(`Fault listing failed to load: ${err.message}`);
				} else {
					resolve(res.body);
				}
			})
		;
	});
}
