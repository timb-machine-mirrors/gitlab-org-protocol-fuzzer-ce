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
				This metric display shows the count of faults found by hour over the course 
				of the fuzzing run. 
				This is the count of all faults found, not just unique buckets.
			</p>
			<canvas className="chart chart-bar"
					// labels="vm.FaultsOverTimeLabels"
					data="vm.FaultsOverTimeData">
			</canvas>

		</div>
	}
}

export default Metrics;
