import React = require('react');
import Icon = require('react-fa');
import { Component, Props } from 'react';
import { Dispatch } from 'redux';
import { connect } from 'redux-await';
import { Button, ButtonToolbar } from 'react-bootstrap';
import { BootstrapTable, TableHeaderColumn } from 'react-bootstrap-table';
import { actions } from 'redux-router5';

import { Job } from '../models/Job';
import { FaultListState, FaultSummary } from '../models/Fault';
import { startPolling, stopPolling } from '../redux/modules/FaultList';
import { R } from '../routes';
import { formatDate } from '../utils';

interface FaultsTableProps extends Props<FaultsTable> {
	limit?: number;
	// injected
	job?: Job;
	faults?: FaultListState;
	dispatch?: Dispatch;
}

@connect(state => ({ 
	job: state.job,
	faults: state.faults
}))
class FaultsTable extends Component<FaultsTableProps, {}> {
	componentDidMount() {
		this.props.dispatch(startPolling());
	}

	componentWillUnmount(): void {
		this.props.dispatch(stopPolling());
	}
	
	render() {
		const { limit, faults } = this.props;
		const data = limit ? _.takeRight(faults.data, limit) : faults.data;
		return <div>
			<BootstrapTable data={data}
				trClassName='pointer'
				striped={true}
				hover={true}
				condensed={true}
				pagination={!limit}
				keyField='iteration'
				selectRow={{
					mode: 'radio', 
					clickToSelect: true, 
					hideSelectColumn: true
				}}
				options={{
					sortName: 'iteration',
					sortOrder: limit ? 'desc' : 'asc',
					onRowClick: this.onRowClick
				}}>
				<TableHeaderColumn dataField="iteration" dataSort={true}>
					#
				</TableHeaderColumn>
				<TableHeaderColumn dataField="timeStamp" dataSort={true} dataFormat={formatDate}>
					When
				</TableHeaderColumn>
				<TableHeaderColumn dataField="source" dataSort={true}>
					Monitor
				</TableHeaderColumn>
				<TableHeaderColumn dataField="exploitability" dataSort={true}>
					Risk
				</TableHeaderColumn>
				<TableHeaderColumn dataField="majorHash" dataSort={true}>
					Major Bucket
				</TableHeaderColumn>
				<TableHeaderColumn dataField="minorHash" dataSort={true}>
					Minor Bucket
				</TableHeaderColumn>
				<TableHeaderColumn dataField="archiveUrl" dataFormat={this.formatDownload}>
					Download
				</TableHeaderColumn>
			</BootstrapTable>
		</div>
	}

	formatDownload(archiveUrl) {
		return <a href={archiveUrl}>Download</a>
	}

	onRowClick = (fault: FaultSummary) => {
		const { job, dispatch } = this.props;
		const to = R.JobFaultsDetail.name;
		const params = { job: job.id, fault: fault.id };
		dispatch(actions.navigateTo(to, params));
	}
}

export default FaultsTable;
