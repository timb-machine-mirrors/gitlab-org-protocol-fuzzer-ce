import React = require('react');
import { Component, Props } from 'react';
import { connect } from 'redux-await';
import { Dispatch } from 'redux';

import { Route } from '../../models/Router';
import { Job } from '../../models/Job';
import Segment from '../../components/Segment';
import { startPolling, stopPolling } from '../../redux/modules/Job';

interface JobComponentProps extends Props<JobComponent> {
	// injected
	route?: Route;
	job?: Job;
	dispatch?: Dispatch;
}

@connect(state => ({
	route: state.router.route,
	job: state.job
}))
class JobComponent extends Component<JobComponentProps, {}> {
	componentDidMount(): void {
		const { job } = this.props.route.params;
		this.props.dispatch(startPolling(job));
	}

	componentWillUnmount(): void {
		this.props.dispatch(stopPolling());
	}

	render() {
		const { job } = this.props;
		if (job.name)
			return <Segment part={2} />
		return <div />
	}
}

export default JobComponent;
