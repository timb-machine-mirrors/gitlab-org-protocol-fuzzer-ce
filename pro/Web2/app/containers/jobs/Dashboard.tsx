import React = require('react');
import { Component, Props } from 'react';
import { connect } from 'react-redux';
import { Alert, Button, ButtonToolbar, Row, Col } from 'react-bootstrap';
import Icon = require('react-fa');
import moment = require('moment');

import { R } from '../../routes';
import { Route } from '../../models/Router';
import { Job, JobStatus, JobMode } from '../../models/Job';
import FaultsTable from '../../components/FaultsTable';
import { formatDate } from '../../utils';

interface DashboardProps extends Props<Dashboard> {
	// injected
	route?: Route;
	job?: Job;
}

const statusTable = {
	[JobStatus.StartPending]    : "Peach is starting...",
	[JobStatus.PausePending]    : "Peach is pausing...",
	[JobStatus.ContinuePending] : "Peach is continuing...",
	[JobStatus.StopPending]     : "Peach is stopping...",
	[JobStatus.KillPending]     : "Peach is aborting...",
	[JobStatus.Starting]        : "Peach is starting...",
	[JobStatus.Paused]          : "Peach is currently paused.",
	[JobStatus.Running]: {
		[JobMode.Fuzzing]       : "Peach is currently fuzzing...",
		[JobMode.Reproducing]   : "Fault detected, attempting to reproduce.",
		[JobMode.Searching]     : "Fault detected. Peach is searching test cases leading to the fault. This action could take a while to complete.",
	},
	[JobStatus.Stopping]: {
		"default"               : "Peach is stopping...",
		[JobMode.Reporting]     : "Generating report..."
	}
};

@connect(state => ({ 
	route: state.router.route,
	job: state.job
}))
class Dashboard extends Component<DashboardProps, {}> {
	render() {
		const { job } = this.props;
		const showLimited = false;
		const showStatus = true;
		const showCommands = true;

		return <div>
			{showLimited &&
				<Alert bsStyle="warning">
					<strong>Note: </strong> &nbsp;
					The current Pit was specified from the command line,
					which limits activities to viewing faults that occur.
					You can stop the current job by stopping the Peach process responsible for this job.
				</Alert>
			}

			{showStatus &&
				<Alert bsStyle="info">
					{this.renderStatus(job)}
				</Alert>
			}

			<Row>
				<Col md={2} />
				<Col md={8} className="infobox-container">
					{this.renderInfobox(formatDate(job.startDate), "Start Time") }
					{this.renderInfobox(this.runningTime, "Running Time")}
					{this.renderInfobox(job.speed, "Test Cases/Hour") }
					{this.renderInfobox(job.seed, "Seed") }
					{this.renderInfobox(job.iterationCount, "Test Cases Executed") }
					{this.renderInfobox(job.faultCount, "Total Faults", 'red') }
				</Col>
				<Col md={2} />
			</Row>

			<div className="space-6"></div>

			<Row className="text-center">
				{showCommands &&
					<ButtonToolbar>
						<Button bsStyle="success">
							<Icon name="play" /> &nbsp; Start
						</Button>
						<Button bsStyle="primary">
							<Icon name="pause" /> &nbsp; Pause
						</Button>
						<Button bsStyle="danger">
							<Icon name="stop" /> &nbsp; Stop
						</Button>
					</ButtonToolbar>
				}

				{!showCommands && 
					<ButtonToolbar>
						<Button bsStyle="primary">
							<Icon name="edit" /> &nbsp; Edit Configuration
						</Button>
						<Button bsStyle="primary">
							<Icon name="replay" /> &nbsp; Replay Job
						</Button>
					</ButtonToolbar>
				}
			</Row>
			
			<hr />

			<h4>Recent Faults</h4>

			<FaultsTable limit={10} />
		</div>
	}

	renderStatus(data: Job) {
		const result = data.result ? data.result : "This job has completed.";
		if (!data.status)
			return <span />;

		if (data.status === JobStatus.Stopped) {
			return (
				<span>
					{result}
					&nbsp;
					{data && data.reportUrl &&
						<span>
							Click &nbsp;
							<a href={data.reportUrl}>
								<Icon name="file-pdf-o" size="lg" /> here 
							</a>
							&nbsp; to view the final report.
						</span>
					}
				</span>
			)
		}

		let next = statusTable[data.status];
		if (_.isString)
			return next;

		return _.get(next, data.mode, next['default']);
	}

	renderInfobox(data, content: string, color: string = 'blue') {
		const klass = `infobox infobox-${color}`;
		const final = _.isNull(data) ? '---' : data;
		return <div className={klass}>
			<div className="infobox-data">
				<span className="infobox-data-number">
					{final}
				</span>
				<div className="infobox-content">
					{content}
				</div>
			</div>
		</div>
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
}

export default Dashboard;
