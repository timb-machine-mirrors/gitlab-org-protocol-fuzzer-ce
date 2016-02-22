import React = require('react');
import { Component, Props } from 'react';
import { Dispatch } from 'redux';
import { connect } from 'redux-await';
import { BootstrapTable, TableHeaderColumn } from 'react-bootstrap-table';

import { Job } from '../../../models/Job';
import { MetricsState } from '../../../models/Metrics';
import { fetchMetric } from '../../../redux/modules/Metrics';

interface MetricsProps extends Props<Metrics> {
	// injected
	job?: Job;
	metrics?: MetricsState;
	dispatch?: Dispatch;
}

@connect(state => ({
	job: state.job,
	metrics: state.metrics
}))
class Metrics extends Component<MetricsProps, {}> {
	componentDidMount() {
		const { job, metrics, dispatch } = this.props;
		dispatch(fetchMetric(job, 'faultTimeline'));
	}

	render() {
		return <div>
			<p>
				This metric display shows the count of faults found by hour over the course
				of the fuzzing run.
				This is the count of all faults found, not just unique buckets.
			</p>
			<canvas className='chart chart-bar'
					// labels='vm.FaultsOverTimeLabels'
					data='vm.FaultsOverTimeData'>
			</canvas>

		</div>;
	}
}

export default Metrics;
