function thunk({ dispatch, getState }) {
	return next => action =>
		_.isFunction(action) ?
			action(dispatch, getState) :
			next(action);
}

export default thunk;
