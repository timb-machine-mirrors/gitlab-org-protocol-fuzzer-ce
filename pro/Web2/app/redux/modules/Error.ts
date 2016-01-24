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
	} else if (error) {
		return error;
	}
	return state;
}

export class Actions {
	constructor(private dispatch: Dispatch) {
	}

	clear() {
		this.dispatch({ type: types.ERROR_RESET });
	}
}
