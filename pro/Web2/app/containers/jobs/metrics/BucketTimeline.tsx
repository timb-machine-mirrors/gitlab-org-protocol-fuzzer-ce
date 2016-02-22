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
		const { job, dispatch } = this.props;
		dispatch(fetchMetric(job, 'bucketTimeline'));
	}

	render() {
		return <div>
			<p>
				This metric display shows a timeline with new fault buckets listed,
				and total number of times the bucket was found during the fuzzing session.
			</p>

			<p>
				Drag the timeline left or right to move through time.
				Zoom in and out with the mouse wheel.
			</p>

			{/*
			<vis-timeline ng-show='vm.BucketTimelineLoaded' 
				data='vm.BucketTimelineData'
				options='vm.BucketTimelineOptions'>
			</vis-timeline>
			*/}

			<script type='text/ng-template'
					id='bucketTimelineItem.html'>
				<div>
					<a href='{{item.data.href}}'>
						item.data.label
					</a>
					<br />
					Faults: item.data.faultCount
					<br />
					1st Iteration: item.data.iteration
					<br />
				</div>
			</script>
		</div>;
	}
}

export default Metrics;
