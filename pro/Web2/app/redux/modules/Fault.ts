import { Dispatch } from 'redux';
import { AWAIT_MARKER } from 'redux-await';
import superagent = require('superagent');

import { FaultState, FaultSummary, FaultDetail } from '../../models/Fault';
import RootState from '../../models/Root';
import { MakeEnum } from '../../utils';
import { doFetch as fetchFaults } from './FaultList';

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

export function fetch(fault: FaultSummary = null) {
	return async (dispatch: Dispatch, getState: Function) => {
		if (fault) {
			return fetchAction(dispatch, fault);
		}

		const state: RootState = getState();
		let { faults } = state;
		const { router, job } = state;
		const params = router.route.params;
		const iteration = parseInt(params.fault);
		if (_.isEmpty(faults)) {
			faults = await fetchFaults(job);
		}
		findFault(dispatch, faults, iteration);
	}
}

function findFault(dispatch: Dispatch, faults: FaultSummary[], iteration: number) {
	const found = _.find(faults, { iteration });
	if (found) {
		fetchAction(dispatch, found);
	} else {
		console.log('not found');
	}
}

function fetchAction(dispatch: Dispatch, fault: FaultSummary) {
	dispatch({
		type: types.FAULT_FETCH,
		AWAIT_MARKER,
		payload: {
			fault: doFetch(fault)
		}
	})
}

async function doFetch(fault: FaultSummary) {
	return new Promise<FaultDetail>((resolve, reject) => {
		superagent.get(fault.faultUrl)
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
