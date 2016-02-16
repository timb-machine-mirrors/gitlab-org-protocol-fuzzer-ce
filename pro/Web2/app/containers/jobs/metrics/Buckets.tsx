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
		dispatch(fetchMetric(job, 'buckets'));
	}

	render() {
		const { buckets } = this.props.metrics;
		return (
			<div>
				<p>
					This metric display shows the buckets encountered during the fuzzing job.
				</p>
				<BootstrapTable data={buckets}
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
					<TableHeaderColumn dataField="id" isKey hidden={true} />
					<TableHeaderColumn dataField="bucket" dataSort={true}>
						Fault Bucket
					</TableHeaderColumn>
					<TableHeaderColumn dataField="mutator" dataSort={true}>
						Mutator
					</TableHeaderColumn>
					<TableHeaderColumn dataField="element" dataSort={true}>
						Element
					</TableHeaderColumn>
					<TableHeaderColumn dataField="iterationCount" dataSort={true}>
						Test Cases
					</TableHeaderColumn>
					<TableHeaderColumn dataField="faultCount" dataSort={true}>
						Faults
					</TableHeaderColumn>
				</BootstrapTable>
			</div>
		)
	}
}

export default Metrics;
