import superagent = require('superagent');
import { AWAIT_MARKER } from 'redux-await';

import { FaultSummary, FaultDetail } from '../../models/Fault';
import RootState from '../../models/Root';
import { MakeEnum } from '../../utils';

const types = {
	FAULT_FETCH: ''
};
MakeEnum(types);

export default function reducer(state: FaultDetail = null, action): FaultDetail {
	switch (action.type) {
		case types.FAULT_FETCH:
			return action.payload.fault;
		default:
			return state;
	}
}

export function fetchFault(params) {
	return {
		type: types.FAULT_FETCH,
		AWAIT_MARKER,
		payload: {
			fault: doFetch(params)
		}
	}
}

function doFetch(params) {
	const { job, fault } = params;
	const url = `/p/jobs/${job}/faults/${fault}`;
	return new Promise<FaultDetail>((resolve, reject) => {
		superagent.get(url)
			.accept('json')
			.end((err, res) => {
				if (err) {
					reject(`Fault failed to load: ${err.message}`);
				} else {
					resolve(res.body);
				}
			})
		;
	});
}
