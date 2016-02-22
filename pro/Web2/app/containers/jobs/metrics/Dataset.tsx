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
		dispatch(fetchMetric(job, 'dataset'));
	}

	render() {
		const { dataset } = this.props.metrics;
		return <div>
			<p>
				This metric display shows statistics related to the use of two or more data sets
				in the fuzzing session.
				This is useful to determine the origin of unique buckets and also faults in terms
				of the data sources used in mutating.
			</p>

			<BootstrapTable data={dataset}
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
				<TableHeaderColumn dataField='dataset' dataSort={true}>
					Data Set
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
