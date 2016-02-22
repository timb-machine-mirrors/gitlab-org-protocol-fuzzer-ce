import React = require('react');
import Icon = require('react-fa');
import moment = require('moment');
import { Component, Props } from 'react';
import { connect } from 'react-redux';
import { Dispatch } from 'redux';
import { Alert, Button, ButtonToolbar, Row, Col } from 'react-bootstrap';

import { R } from '../../containers';
import { Job, JobStatus, JobMode } from '../../models/Job';
import { stopJob, pauseJob, continueJob, killJob } from '../../redux/modules/Job';
import FaultsTable from '../../components/FaultsTable';
import LinkContainer from '../../components/LinkContainer';
import { formatDate } from '../../utils';

interface DashboardProps extends Props<Dashboard> {
	// injected
	job?: Job;
	dispatch?: Dispatch;
}

const statusTable = {
	[JobStatus.StartPending]    : 'Peach is starting...',
	[JobStatus.PausePending]    : 'Peach is pausing...',
	[JobStatus.ContinuePending] : 'Peach is continuing...',
	[JobStatus.StopPending]     : 'Peach is stopping...',
	[JobStatus.KillPending]     : 'Peach is aborting...',
	[JobStatus.Starting]        : 'Peach is starting...',
	[JobStatus.Paused]          : 'Peach is currently paused.',
	[JobStatus.Running]: {
		[JobMode.Fuzzing]       : 'Peach is currently fuzzing...',
		[JobMode.Reproducing]   : 'Fault detected, attempting to reproduce.',
		[JobMode.Searching]     : 'Fault detected. Peach is searching test cases leading to the fault. This action could take a while to complete.',
	},
	[JobStatus.Stopping]: {
		'default'               : 'Peach is stopping...',
		[JobMode.Reporting]     : 'Generating report...'
	}
};

@connect(state => ({
	job: state.job
}))
class Dashboard extends Component<DashboardProps, {}> {
	render() {
		const { job } = this.props;
		const stopButton = (job.status === JobStatus.Stopping) ?
			{ icon: 'power-off', label: 'Abort' } :
			{ icon: 'stop'     , label: 'Stop'  };
		const showLimited = _.isEmpty(job.pitUrl);
		const pitId = _.last(job.pitUrl.split('/'));
		const editParams = {
			pit: pitId
		};
		const replayParams = {
			pit: pitId,
			seed: job.seed,
			start: job.rangeStart,
			stop: job.rangeStop
		};

		return <div>
			{showLimited &&
				<Alert bsStyle='warning'>
					<strong>Note: </strong> &nbsp;
					The current Pit was specified from the command line,
					which limits activities to viewing faults that occur.
					You can stop the current job by stopping the Peach process responsible for this job.
				</Alert>
			}

			{job.status &&
				<Alert bsStyle='info'>
					{this.renderStatus(job)}
				</Alert>
			}

			<Row>
				<Col md={2} />
				<Col md={8} className='infobox-container'>
					{this.renderInfobox(formatDate(job.startDate), 'Start Time') }
					{this.renderInfobox(this.runningTime, 'Running Time')}
					{this.renderInfobox(job.speed, 'Test Cases/Hour') }
					{this.renderInfobox(job.seed, 'Seed') }
					{this.renderInfobox(job.iterationCount, 'Test Cases Executed') }
					{this.renderInfobox(job.faultCount, 'Total Faults', 'red') }
				</Col>
				<Col md={2} />
			</Row>

			<div className='space-6'></div>

			<Row className='text-center'>
				{job.status !== JobStatus.Stopped &&
					<ButtonToolbar>
						<Button bsStyle='success'
							onClick={this.onContinue}>
							<Icon name='play' /> &nbsp; Start
						</Button>
						<Button bsStyle='primary'
							onClick={this.onPause}>
							<Icon name='pause' /> &nbsp; Pause
						</Button>
						<Button bsStyle='danger'
							onClick={this.onStop}>
							<Icon name={stopButton.icon} /> &nbsp; {stopButton.label}
						</Button>
					</ButtonToolbar>
				}

				{job.status === JobStatus.Stopped &&
					<ButtonToolbar>
						<LinkContainer to={R.Pit} params={editParams}>
							<Button bsStyle='primary'>
								<Icon name='edit' /> &nbsp; Edit Configuration
							</Button>
						</LinkContainer>
						<LinkContainer to={R.Pit} params={replayParams}>
							<Button bsStyle='primary'>
								<Icon name='repeat' /> &nbsp; Replay Job
							</Button>
						</LinkContainer>
					</ButtonToolbar>
				}
			</Row>

			<hr />

			<h4>Recent Faults</h4>

			<FaultsTable limit={10} />
		</div>;
	}

	renderStatus(job: Job) {
		if (job.status === JobStatus.Stopped) {
			const result = _.get(job, 'result', 'This job has completed.');
			return <span>
				{result}
				&nbsp;
				{job && job.reportUrl &&
					<span>
						Click &nbsp;
						<a href={job.reportUrl}>
							<Icon name='file-pdf-o' size='lg' /> here
						</a>
						&nbsp; to view the final report.
					</span>
				}
			</span>;
		}

		const next = statusTable[job.status];
		if (_.isString(next))
			return next;

		return _.get(next, job.mode, next['default']);
	}

	renderInfobox(job, content: string, color: string = 'blue') {
		const klass = `infobox infobox-${color}`;
		const final = _.isNull(job) ? '---' : job;
		return <div className={klass}>
			<div className='infobox-data'>
				<span className='infobox-data-number'>
					{final}
				</span>
				<div className='infobox-content'>
					{content}
				</div>
			</div>
		</div>;
	}

	get runningTime() {
		const { job } = this.props;
		if (!job.runtime) {
			return null;
		}

		const duration = moment.duration(job.runtime, 'seconds');
		const days = Math.floor(duration.asDays());
		const hours = _.padStart(duration.hours().toString(), 2, '0');
		const minutes = _.padStart(duration.minutes().toString(), 2, '0');
		const seconds = _.padStart(duration.seconds().toString(), 2, '0');

		if (duration.asDays() >= 1) {
			return `${days}d ${hours}h ${minutes}m`;
		} else {
			return `${hours}h ${minutes}m ${seconds}s`;
		}
	}

	onPause = () => {
		const { job, dispatch } = this.props;
		dispatch(pauseJob(job));
	};

	onContinue = () => {
		const { job, dispatch } = this.props;
		dispatch(continueJob(job));
	};

	onStop = () => {
		const { job, dispatch } = this.props;
		dispatch(job.status === JobStatus.Stopping ?
			killJob(job) : stopJob(job)
		);
	};
}

export default Dashboard;
