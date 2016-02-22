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
		dispatch(fetchMetric(job, 'states'));
	}

	render() {
		const { states } = this.props.metrics;
		return <div>
			<p>
				This metric display presents statistics that are relevant for pits that
				have state models with more than two or more states.
				This display shows the number of times a specific state occurred during
				the fuzzing session.
				Seldom-used states might hide issues or indicate a problem.
			</p>
			<BootstrapTable data={states}
				striped={true}
				hover={true}
				condensed={true}
				pagination={true}
				options={{
					sortName: 'state',
					sortOrder: 'asc',
					paginationSize: 25,
					sizePerPageList: [10, 25, 50, 100]
				}}>
				<TableHeaderColumn dataField='id' isKey hidden={true} />
				<TableHeaderColumn dataField='state' dataSort={true}>
					State
				</TableHeaderColumn>
				<TableHeaderColumn dataField='executionCount' dataSort={true}>
					Executions
				</TableHeaderColumn>
			</BootstrapTable>
		</div>;
	}
}

export default Metrics;
