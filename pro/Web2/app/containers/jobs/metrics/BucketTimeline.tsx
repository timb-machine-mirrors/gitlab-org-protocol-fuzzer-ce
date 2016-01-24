import React = require('react');
import { Component, Props } from 'react';
import { connect } from 'react-redux';

import { R } from '../../../routes';
import { Route } from '../../../models/Router';

interface MetricsProps extends Props<Metrics> {
	route?: Route;
}

@connect(state => ({ route: state.router.route }))
class Metrics extends Component<MetricsProps, {}> {
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
			<vis-timeline ng-show="vm.BucketTimelineLoaded" 
			              data="vm.BucketTimelineData" 
			              options="vm.BucketTimelineOptions">
			</vis-timeline>
			*/}

			<script type="text/ng-template"
			        id="bucketTimelineItem.html">
				<div>
					<a href="{{item.data.href}}">
						item.data.label
					</a>
					<br />
					Faults: item.data.faultCount
					<br />
					1st Iteration: item.data.iteration
					<br />
				</div>
			</script>
		</div>
	}
}

export default Metrics;
