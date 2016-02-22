import React = require('react');
import Icon = require('react-fa');
import { Component, Props } from 'react';
import { Dispatch } from 'redux';
import { connect } from 'redux-await';
import { Alert, Button, ButtonToolbar, Input, Panel } from 'react-bootstrap';
import { actions } from 'redux-router5';
import { reduxForm, ReduxFormProps, FormData } from 'redux-form';

import { R } from '../../containers';
import { Route } from '../../models/Router';
import { Pit, Parameter, ParameterType } from '../../models/Pit';
import { JobRequest, Job } from '../../models/Job';
import Link from '../../components/Link';
import LinkContainer from '../../components/LinkContainer';
import { api } from '../../services';

interface DashboardProps extends Props<Dashboard> {
	// injected
	route?: Route;
	pit?: Pit;
	formProps?: ReduxFormProps;
	dispatch?: Dispatch;
	statuses?: any;
}

interface DashboardState {
	showCfgHelp?: boolean;
	showStartHelp?: boolean;
}

@connect(state => ({
	route: state.router.route,
	pit: state.pit
}))
@reduxForm({
	form: 'PitDashboard',
	fields: [
		'seed',
		'start',
		'stop'
	],
	propNamespace: 'formProps'
}, state => ({ initialValues: state.router.route.params }))
class Dashboard extends Component<DashboardProps, DashboardState> {
	constructor(props: DashboardProps, context) {
		super(props, context);
		const showCfgHelp = localStorage.getItem('showCfgHelp');
		const showStartHelp = localStorage.getItem('showStartHelp');
		this.state = {
			showCfgHelp: _.isNull(showCfgHelp) ? true : JSON.parse(showCfgHelp),
			showStartHelp: _.isNull(showStartHelp) ? true : JSON.parse(showStartHelp)
		};
	}

	render() {
		const { pit, formProps, route: { params } } = this.props;
		const { showCfgHelp, showStartHelp } = this.state;
		const { handleSubmit, fields } = formProps;
		const name = pit.name ? pit.name : 'Loading...';

		return <div>
			{!pit.isConfigured &&
				<Alert bsStyle='danger'>
					<strong>Error!</strong>
					&nbsp;
					The currently selected Pit has required configuration variables that must be set.
					&nbsp;
					<Link to={R.PitWizard} params={params}>
						Pit Configuration Quick Start
					</Link>
				</Alert>
			}

			{pit.isConfigured && !pit.hasMonitors &&
				<Alert bsStyle='warning'>
					<strong>Warning!</strong>
					&nbsp;
					The currently selected Pit should be configured for monitoring the environment.
					&nbsp;
					<Link to={R.PitWizard} params={params}>
						Pit Configuration Quick Start
					</Link>
				</Alert>
			}

			{pit.isConfigured && pit.hasMonitors &&
				<Alert bsStyle='success'>
					The Pit is configured and ready for use.
					Click the START button below to begin fuzzing.
				</Alert>
			}

			<p>
				This configuration uses the <code>{name}</code> pit and
				includes configuration data for your test setup.
			</p>

			<Panel bsStyle='default'
				header={this.renderHeader('showCfgHelp', 'Configuration Options') }>
				{showCfgHelp && this.renderCfgHelp()}

				<ButtonToolbar className='center'>
					<LinkContainer to={R.PitWizard} params={params}>
						<Button bsStyle='primary'>
							<Icon name='rocket' />
							&nbsp; Quick Start Wizard
						</Button>
					</LinkContainer>

					<LinkContainer to={R.PitAdvancedVariables} params={params}>
						<Button bsStyle='primary'>
							<Icon name='wrench' />
							&nbsp; Configure Variables
						</Button>
					</LinkContainer>

					<LinkContainer to={R.PitAdvancedMonitoring} params={params}>
						<Button bsStyle='primary'>
							<Icon name='wrench' />
							&nbsp; Configure Monitoring
						</Button>
					</LinkContainer>
				</ButtonToolbar>
			</Panel>

			<Panel bsStyle='default'
				header={this.renderHeader('showStartHelp', 'Start Options') }>
				<form className='form-horizontal'>
					{showStartHelp && this.renderStartHelp() }

					<Input type='number'
						label='Seed'
						labelClassName='col-sm-4'
						wrapperClassName='col-sm-6'
						placeholder='Random Seed'
						{...fields['seed']}
						// peach-range
						// peach-range-min='0'
						// peach-range-max='4294967295'
					/>

					<Input type='number'
						label='Start Test Case'
						labelClassName='col-sm-4'
						wrapperClassName='col-sm-6'
						placeholder='1'
						{...fields['start']}
						// peach:range
						// peach:range-min='1'
						// peach:range-max='4294967295'
					/>

					<Input type='number'
						label='Stop Test Case'
						labelClassName='col-sm-4'
						wrapperClassName='col-sm-6'
						placeholder='Default'
						{...fields['stop']}
						// peach:range
						// peach:range-min='1'
						// peach:range-max='4294967295'
					/>

					<ButtonToolbar className='center'>
						<Button bsStyle='primary'
							disabled={!pit.isConfigured}
							onClick={handleSubmit(this.onStart)}>
							<Icon name='play' />
							&nbsp; Start
						</Button>
					</ButtonToolbar>
				</form>
			</Panel>
		</div>;
	}

	renderHeader(name: string, title: string) {
		const toggle = this.state[name];
		return <span>
			<span>
				{title}
			</span>
			<span className= 'pull-right'>
				<Button bsSize='xs'
					onClick={() => this.onToggleHelp(name)}
					active={toggle}>
					<Icon name='question-circle' /> &nbsp; {toggle ? 'Hide' : 'Help'}
				</Button>
			</span>
		</span>;
	}

	renderCfgHelp() {
		return <div>
			<p>
				Set or change the test configuration using the following buttons:
			</p>
			<dl className='dl-horizontal'>
				<dt>
					Quick Start Wizard
				</dt>
				<dd>
					Leads a structured question &amp; answer session that covers typical configurations.
					<br/>
					This choice is recommended for novice users and for simple configurations.
				</dd>
				<dt>
					Configure Variables
				</dt>
				<dd>
					A list of items needed by the configuration,
					including global pit variables and pit-specific information, such as target IP addresses.
				</dd>
				<dt>
					Configure Monitoring
				</dt>
				<dd>
					A list of the agents and monitors defined for the test configuration.
					Click this button to select and update the agents,
					monitors and associated monitor settings for the test configuration.
					<br/>
					This choice provides the most flexibility in implementing
					fault detection, data collection, and automation.
				</dd>
			</dl>
		</div>;
	}

	renderStartHelp() {
		return <div>
			<p>
				The Start Options specify the test cases that start and end a fuzzing job,
				and identify a seed for generating mutated data.
				For a new fuzzing job, the default values are usually appropriate to use,
				although you can select other values to use.
			</p>

			<p>
				If you want to replay a fuzzing run, you need to use the same Start Options
				as in the original fuzzing job.
				You can obtain these values by
				1) selecting the fuzzing run you want to replay,
				and 2) click the Replay button on that job page.
			</p>

			<p>
				For replay, changing the Start Options has the following effects:
			</p>
			<ul>
				<li>
					Change the Seed.
					This changes the mutated data values used in all test cases.
					The result is definitely a new job.
				</li>
				<li>
					Change the Start or Stop Test Case.
					This changes the test cases (first and/or last) to execute in the job,
					thereby lengthening or shortening the total number of test cases executed in the job.
					Note that changing the test cases (first and last) for the job can help validate an issue
					or the fix for an issue.
					However, the result might not produce the intended results.
				</li>
			</ul>

			<p>
				Click "Start” to begin the fuzzing job.
			</p>

			<br/>
		</div>;
	}

	onToggleHelp(name: string) {
		const value = !this.state[name];
		localStorage.setItem(name, JSON.stringify(value));
		this.setState({
			[name]: value
		});
	}

	onStart = (data: FormData) => {
		const { pit, dispatch } = this.props;
		const { seed, start, stop } = data;
		const request: JobRequest = {
			pitUrl: pit.pitUrl,
			seed,
			rangeStart: start,
			rangeStop: stop
		};
		const to = R.Job.name;
		return new Promise((resolve, reject) => {
			api.startJob(request)
				.then(job => {
					resolve();
					const params = { job: job.id };
					dispatch(actions.navigateTo(to, params));
				}, reason => {
					reject({ _error: reason.message });
				})
			;
		});
	};
}

export default Dashboard;
