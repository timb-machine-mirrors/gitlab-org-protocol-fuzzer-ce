import React = require('react');
import { Component, Props } from 'react';
import { connect } from 'react-redux';
import { Dispatch } from 'redux';

import { clearError } from '../redux/modules/Error';

interface ErrorProps extends Props<Error> {
	// injected
	error?: string;
	dispatch?: Dispatch;
}

@connect(state => ({ error: state.error }))
class Error extends Component<ErrorProps, {}> {
	render() {
		const error = this.props.error || 'An unknown error has occured.';
		return (
			<div className="text-center">
				<h3>
					We're sorry.
				</h3>
				<p>
					<img style={{ width: 500 }}
						src="/img/peach-happy-sad.svg" />
				</p>
				
				<hr />
				
				<h4 style={{ whiteSpace: 'pre-line' }}>
					{error}
				</h4>
			</div>
		)
	}
}

export default Error;
