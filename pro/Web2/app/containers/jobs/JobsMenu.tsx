import React = require('react');
import { Component, Props } from 'react';

import { Menu, MenuItem } from '../../components/Menu';
import { R } from '../../routes';

class JobsMenu extends Component<Props<JobsMenu>, {}> {
	render() {
		return <Menu>
			<MenuItem to={R.Job} />
			<MenuItem to={R.JobFaults} />
			<MenuItem to={R.JobMetrics}>
				<MenuItem to={R.JobMetricsBucketTimeline} />
				<MenuItem to={R.JobMetricsFaultTimeline} />
				<MenuItem to={R.JobMetricsMutators} />
				<MenuItem to={R.JobMetricsElements} />
				<MenuItem to={R.JobMetricsStates} />
				<MenuItem to={R.JobMetricsDataset} />
				<MenuItem to={R.JobMetricsBuckets} />
			</MenuItem>
		</Menu>
	}
}

export default JobsMenu;
