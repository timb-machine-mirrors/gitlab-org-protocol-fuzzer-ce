import React = require('react');
import { Component, Props } from 'react';
import { Dispatch } from 'redux';
import { connect } from 'redux-await';
import { Button, ButtonToolbar } from 'react-bootstrap';
import Icon = require('react-fa');
import { BootstrapTable, TableHeaderColumn } from 'react-bootstrap-table';
import { actions } from 'redux-router5';

import { Job } from '../models/Job';
import { fetch } from '../redux/modules/JobList';
import { R } from '../routes';
import { formatDate } from '../utils';

interface JobsTableProps extends Props<JobsTable> {
	limit?: number;
	// injected
	jobs?: Job[];
	dispatch?: Dispatch;
}

@connect(state => ({ jobs: state.jobs }))
class JobsTable extends Component<JobsTableProps, {}> {
	componentDidMount() {
		this.props.dispatch(fetch());
	}
	
	render() {
		const { limit, jobs } = this.props;
		const data = limit ? _.takeRight(jobs, limit) : jobs;
		return (
			<div>
				<BootstrapTable data={data}
					trClassName='pointer'
					striped={true}
					hover={true}
					condensed={true}
					pagination={!limit}
					keyField='id'
					selectRow={{
						mode: 'radio', 
						clickToSelect: true, 
						hideSelectColumn: true
					}}
					options={{
						sortName: 'startDate',
						sortOrder: 'desc',
						onRowClick: this.onRowClick
					}}>
					<TableHeaderColumn dataField="name" dataSort={true} width="350">
						Name
					</TableHeaderColumn>
					<TableHeaderColumn dataField="status" dataSort={true}>
						Status
					</TableHeaderColumn>
					<TableHeaderColumn dataField="startDate" dataSort={true} dataFormat={formatDate}>
						Start Time
					</TableHeaderColumn>
					<TableHeaderColumn dataField="stopDate" dataSort={true} dataFormat={formatDate}>
						Stop Time
					</TableHeaderColumn>
					<TableHeaderColumn dataField="iterationCount" dataSort={true}>
						Test Case Count
					</TableHeaderColumn>
					<TableHeaderColumn dataField="faultCount" dataSort={true}>
						Total Faults
					</TableHeaderColumn>
					<TableHeaderColumn dataFormat={this.formatActions}>
						Actions
					</TableHeaderColumn>
				</BootstrapTable>
			</div>
		)
	}

	formatActions() {
		return (
			<ButtonToolbar>
				<Button bsStyle="default" bsSize="xs">
					<Icon name="file-pdf-o" size="lg" />
				</Button>
				<Button bsStyle="default" bsSize="xs">
					<Icon name="remove" size="lg" />
				</Button>
			</ButtonToolbar>
		)
	}

	onRowClick = (job: Job) => {
		const to = R.Job.name;
		const params = { job: job.id };
		const { dispatch } = this.props;
		dispatch(actions.navigateTo(to, params));
	}
}

export default JobsTable;
