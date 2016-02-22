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
		dispatch(fetchMetric(job, 'elements'));
	}

	render() {
		const { elements } = this.props.metrics;
		return <div>
			<p>
				This metric display shows statistics for all of the elements in your Pit.
			</p>
			<BootstrapTable data={elements}
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
				<TableHeaderColumn dataField='state' dataSort={true}>
					State
				</TableHeaderColumn>
				<TableHeaderColumn dataField='action' dataSort={true}>
					Action
				</TableHeaderColumn>
				<TableHeaderColumn dataField='element' dataSort={true}>
					Element
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
