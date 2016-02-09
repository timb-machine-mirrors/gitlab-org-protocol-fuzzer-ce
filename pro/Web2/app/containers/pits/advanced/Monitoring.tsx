import React = require('react');
import Icon = require('react-fa');
import { Dispatch } from 'redux';
import { connect } from 'redux-await';
import { Component, Props } from 'react';
import { reduxForm, ReduxFormProps, FormData, FieldProp } from 'redux-form';
import { Alert, Accordion, Row, Col, Button, ButtonToolbar } from 'react-bootstrap';

import { R } from '../../../routes';
import { Route, RouterContext, injectRouter } from '../../../models/Router';
import PitAgent from '../../../components/PitAgent';
import { Pit, AgentsFormData, Parameter, ParameterType } from '../../../models/Pit';
import { validationMessages } from '../../../utils';
import { 
	saveAgents, mapAgentsToView, findMonitorMetadata, validateParameter
} from '../../../redux/modules/Pit';

interface ValidationRule {
	required?: boolean;
	unique?: string[];
	uniqueDefault?: string;
	uniqueIgnore?: RegExp;
}

function validateField(rule: ValidationRule, value: string): string {
	if (rule.required && _.isEmpty(value)) {
		return validationMessages.required;
	}

	if (_.isArray(rule.unique)) {
		const actual = value || rule.uniqueDefault;
		if (!rule.uniqueIgnore || !rule.uniqueIgnore.test(actual)) {
			if (_.includes(rule.unique, actual)) {
				return validationMessages.unique;
			}
		}
		rule.unique.push(actual);
	}
	
	return null;
}

function validate(values: AgentsFormData, props: MonitoringProps): any {
	const { agents } = values;
	const { pit } = props;
	if (!!agents.length) {
		const agentNames = [];
		const agentLocations = [];
		return {
			agents: agents.map(agent => {
				const monitorNames = [];
				return {
					name: validateField({ required: true, unique: agentNames }, agent.name),
					location: validateField({ 
						unique: agentLocations,
						uniqueDefault: 'local://',
						uniqueIgnore: /^local:\/\//
					}, agent.location),
					monitors: agent.monitors.map((monitor, i) => {
						const schema = findMonitorMetadata(pit, monitor.monitorClass);
						return {
							name: validateField({ 
								unique: monitorNames, 
								uniqueDefault: `Monitor${i}` 
							}, monitor.name),
							params: monitor.params.map((param, j) => 
								validateParameter(schema.items[j], param)
							),
							groups: monitor.groups.map((group, j) => ({
								params: group.params.map((param, k) => 
									validateParameter(schema.items[j].items[k], param)
								)
							}))
						}
					})
				};
			})
		}
	}
	return {};
}

interface MonitoringProps extends Props<Monitoring> {
	// injected
	pit?: Pit;
	formProps?: ReduxFormProps;
	dispatch?: Dispatch;
	statuses?: any;
}

interface MonitoringState {
	isSaved?: boolean;
}

@connect(state => ({ pit: state.pit }))
@reduxForm({
	form: 'Monitoring',
	fields: [
		'agents[].name',
		'agents[].location',
		'agents[].monitors[].monitorClass',
		'agents[].monitors[].name',
		'agents[].monitors[].params[]',
		'agents[].monitors[].groups[].params[]'
	],
	propNamespace: 'formProps',
	validate
}, state => ({ initialValues: mapAgentsToView(state.pit) }))
@injectRouter
class Monitoring extends Component<MonitoringProps, MonitoringState> {
	context: RouterContext;
	
	constructor(props, context) {
		super(props, context);
		this.state = {
			isSaved: false
		};
	}

	componentDidUpdate() {
		const { pit, formProps } = this.props;
		const { router } = this.context;
		const pristine = _.isEqual(mapAgentsToView(pit), formProps.values);
		console.log('canDeactivate', pristine);
		// router.canDeactivate(R.PitAdvancedMonitoring.name, pristine);
	}

	render() {
		const { pit, formProps, statuses } = this.props;
		const { isSaved } = this.state;
		const { invalid, submitting, handleSubmit } = formProps;
		const fields = formProps.fields['agents'] as any;
		const pristine = _.isEqual(mapAgentsToView(pit), formProps.values);

		return <form className="form-horizontal" autoComplete="off">
			<p>
				The Monitoring data entry screen defines one or more Agents and one or more Monitors for the Pit.
			</p>
			<p>
				Agents are host processes for monitors and publishers.
				Local agents can reside on the same machine as Peach,
				and can control the test environment through monitors and publishers.
				Remote agents reside on the test target, and can provide remote monitors and publishers.
			</p>
			
			<Row className="margin-bottom-10">
				<Col xs={6}>
					{this.renderStatus(pristine) }
				</Col>
				<Col xs={6} className="text-right">
					<ButtonToolbar>
						<Button bsStyle="danger" bsSize="xs"
							disabled={pristine || invalid || submitting}
							onClick={handleSubmit(this.onSave)}>
							<Icon name='save' /> &nbsp; Save
						</Button>
						<Button bsStyle="info" bsSize="xs"
							disabled={submitting}
							onClick={this.onAddAgent}>
							Add Agent &nbsp; <Icon name='plus' />
						</Button>
					</ButtonToolbar>
				</Col>
			</Row>

			{statuses.pit === 'pending' &&
				<Row>
					<Col xs={12}>
						<Alert bsStyle="info">
							Loading configuration...
							</Alert>
						</Col>
				</Row>
			}
			{statuses.pit === 'success' && !!fields.length && 
				<Row>
					<Col xs={12}>
						<Accordion>
							{fields.map((agent, index) => 
								<PitAgent key={index} 
									pit={pit}
									fields={fields}
									index={index} />
							)}
						</Accordion>
					</Col>
				</Row>
			}
		</form>
	}

	renderStatus(pristine: boolean) {
		const { pit, formProps, statuses } = this.props;
		const { isSaved } = this.state;
		const { invalid } = formProps;
		const fields = formProps.fields['agents'] as any;

		if (statuses.pit === 'success' && !fields.length) {
			return <span className="red">
				<strong>Warning!</strong> &nbsp; No agents have been configured.
			</span>
		} else if(!pristine && invalid) {
			return <span className="red">
				Save is disabled until validation issues are fixed.
			</span>
		} else if (pristine && isSaved && statuses.pit === 'success') {
			return <span className="green">
				Saved successfully.
			</span>
		}
	}

	onSave = (data: FormData) => {
		const { pit, dispatch } = this.props;
		dispatch(saveAgents(pit, data));
		this.setState({ isSaved: true });
	}

	onAddAgent = () => {
		const { formProps } = this.props;
		const fields = formProps.fields['agents'] as any;
		fields.addField();
	}

	onRemoveAgent = (index: number) => {
		const { formProps } = this.props;
		const fields = formProps.fields['agents'] as any;
		fields.removeField(index);
	}
}

export default Monitoring;
