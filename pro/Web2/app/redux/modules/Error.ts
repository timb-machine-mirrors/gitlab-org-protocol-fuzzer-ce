import { SagaCancellationException } from 'redux-saga'
import { Dispatch } from 'redux';

import { MakeEnum } from '../../utils';

const types = {
	ERROR_RESET: ''
};
MakeEnum(types);

export default function reducer(state = null, action): Error {
	const { type, error } = action;
	if (type === types.ERROR_RESET) {
		return null;
	} else if (error && !(error instanceof SagaCancellationException)) {
		console.log('error', error);
		return error;
	}
	return state;
}

export function clearError() {
	return { type: types.ERROR_RESET };
}
