import React = require('react');
import Icon = require('react-fa');
import { Component, Props, MouseEvent } from 'react';
import { Dispatch } from 'redux';
import { connect } from 'redux-await';
import { Button, ButtonToolbar } from 'react-bootstrap';
import { BootstrapTable, TableHeaderColumn } from 'react-bootstrap-table';
import { actions } from 'redux-router5';

import { Job, JobStatus } from '../models/Job';
import { fetchJobs } from '../redux/modules/JobList';
import { deleteJob } from '../redux/modules/Job';
import { R } from '../containers';
import { formatDate } from '../utils';
import ConfirmModal from './ConfirmModal';

interface JobsTableProps extends Props<JobsTable> {
	limit?: number;
	// injected
	jobs?: Job[];
	dispatch?: Dispatch;
}

interface JobsTableState {
	showModal?: boolean;
	job?: Job;
}

@connect(state => ({ jobs: state.jobs }))
class JobsTable extends Component<JobsTableProps, JobsTableState> {
	constructor(props, context) {
		super(props, context);
		this.state = {
			showModal: false,
			job: null
		};
	}

	componentDidMount() {
		this.props.dispatch(fetchJobs());
	}

	render() {
		const { limit, jobs } = this.props;
		const { showModal } = this.state;
		const data = limit ? _.takeRight(jobs, limit) : jobs;
		return <div>
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
				<TableHeaderColumn dataField='name' dataSort={true} width='400'>
					Name
				</TableHeaderColumn>
				<TableHeaderColumn dataField='status' dataSort={true}>
					Status
				</TableHeaderColumn>
				<TableHeaderColumn dataField='startDate' dataSort={true} dataFormat={formatDate}>
					Start Time
				</TableHeaderColumn>
				<TableHeaderColumn dataField='stopDate' dataSort={true} dataFormat={formatDate}>
					Stop Time
				</TableHeaderColumn>
				<TableHeaderColumn dataField='iterationCount' dataSort={true}>
					Test Cases
				</TableHeaderColumn>
				<TableHeaderColumn dataField='faultCount' dataSort={true}>
					Total Faults
				</TableHeaderColumn>
				<TableHeaderColumn dataFormat={this.formatActions}>
					Actions
				</TableHeaderColumn>
			</BootstrapTable>
			{showModal &&
				<ConfirmModal submitPrompt='Delete Job'
					onComplete={this.onRemoveComplete}
				/>
			}
		</div>;
	}

	formatActions = (cell, job: Job) => {
		return <ButtonToolbar>
			{!_.isEmpty(job.reportUrl) &&
				<Button bsStyle='default' bsSize='xs'
					href={job.reportUrl}
					onClick={event => this.onViewReport(event, job) }
					target='_blank'
					uib-tooltip='View Report'>
					<Icon name='file-pdf-o' size='lg' />
				</Button>
			}
			<Button bsStyle='default' bsSize='xs'
				disabled={job.status !== JobStatus.Stopped}
				onClick={event => this.onRemove(event, job)}
				uib-tooltip='Delete Job'>
				<Icon name='remove' size='lg' />
			</Button>
		</ButtonToolbar>;
	};

	onRowClick = (job: Job) => {
		const to = R.Job.name;
		const params = { job: job.id };
		const { dispatch } = this.props;
		dispatch(actions.navigateTo(to, params));
	};

	onViewReport = (event: MouseEvent, job: Job) => {
		event.stopPropagation();
	};

	onRemove = (event: MouseEvent, job: Job) => {
		const { dispatch } = this.props;
		event.preventDefault();
		event.stopPropagation();
		this.setState({ showModal: true, job });
	};

	onRemoveComplete = (result: boolean) => {
		const { dispatch } = this.props;
		const { job } = this.state;
		if (result) {
			dispatch(deleteJob(job));
		}
		this.setState({ showModal: false, job: null });
	};
}

export default JobsTable;
