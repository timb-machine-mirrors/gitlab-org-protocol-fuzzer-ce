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
		dispatch(fetchMetric(job, 'mutators'));
	}

	render() {
		const { mutators } = this.props.metrics;
		return <div>
			<p>
				This metric display shows statistics for each mutator.
			</p>
			<BootstrapTable data={mutators}
				striped={true}
				hover={true}
				condensed={true}
				pagination={true}
				options={{
					sortName: 'faultCount',
					sortOrder: 'desc',
					paginationSize: 25,
					sizePerPageList: [10, 25, 50, 100]
				}}>
				<TableHeaderColumn dataField='id' isKey hidden={true} />
				<TableHeaderColumn dataField='mutator' dataSort={true}>
					Mutator
				</TableHeaderColumn>
				<TableHeaderColumn dataField='elementCount' dataSort={true}>
					Elements
				</TableHeaderColumn>
				<TableHeaderColumn dataField='iterationCount' dataSort={true}>
					Test Cases
				</TableHeaderColumn>
				<TableHeaderColumn dataField='bucketCount' dataSort={true}>
					Buckets
				</TableHeaderColumn>
				<TableHeaderColumn dataField='faultCount' dataSort={true}>
					Faults
				</TableHeaderColumn>
			</BootstrapTable>
		</div>;
	}
}

export default Metrics;
