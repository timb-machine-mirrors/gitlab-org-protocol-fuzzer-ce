import { AWAIT_MARKER } from 'redux-await';

import { FaultSummary, FaultDetail } from '../../models/Fault';
import RootState from '../../models/Root';
import { MakeEnum } from '../../utils';
import { api } from '../../services';

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
			fault: api.fetchFault(params)
		}
	}
}
