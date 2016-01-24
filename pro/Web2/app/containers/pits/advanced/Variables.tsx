import React = require('react');
import Icon = require('react-fa');
import { Dispatch } from 'redux';
import { Component, Props } from 'react';
import { connect } from 'redux-await';
import { Alert, Accordion, Button, ButtonToolbar, Row, Col } from 'react-bootstrap';
import { reduxForm, ReduxFormProps, FormData, FieldProp } from 'redux-form';

import PitDefines from '../../../components/PitDefines';
import { Pit, Parameter, ParameterType, DefinesFormData } from '../../../models/Pit';
import { saveDefines, validateParameter } from '../../../redux/modules/Pit';

function mapValues(pit: Pit): DefinesFormData {
	if (!pit)
		return { groups: [] };

	const groups = pit.definesView;
	return { groups: groups.map(group => ({ params: group.items.map(param => param.value) })) };
}

function makePairs(params: Parameter[], fields: any[]) {
	if (params && params.length && fields && fields.length) {
		return _.zip<any>(params, fields).map(item => ({
			param: item[0] as Parameter,
			fields: item[1].params
		}));
	}
	return [];
}

function validate(values: DefinesFormData, props: VariablesProps): any {
	const { groups } = values;
	if (props.pit && !!groups.length) {
		const params = props.pit.definesView;
		return {
			groups: _.zip<any>(params, groups).map(group => ({
				params: _(_.zip<any>(group[0].items, group[1].params))
					.map(param => validateParameter(param[0], param[1]))
					.value()
			}))
		}
	}
	return {};
}

interface VariablesProps extends Props<Variables> {
	// injected
	pit?: Pit;
	formProps?: ReduxFormProps;
	dispatch?: Dispatch;
	statuses?: any;
}

interface VariablesState {
	isSaved: boolean;
}

@connect(state => ({ pit: state.pit } ))
@reduxForm({
	form: 'Defines',
	fields: ['groups[].params[]'],
	propNamespace: 'formProps',
	validate
}, state => ({ initialValues: mapValues(state.pit) }))
class Variables extends Component<VariablesProps, VariablesState> {
	constructor(props, context) {
		super(props, context);
		this.state = {
			isSaved: false
		}
	}

	render() {
		const { pit, formProps, statuses } = this.props;
		const { isSaved } = this.state;
		const params = pit.definesView;
		const {
			dirty,
			pristine,
			invalid,
			error,
			submitting,
			handleSubmit
		} = formProps;

		const fields = formProps.fields['groups'] as any;
		const pairs = makePairs(params, fields);

		return <div>
			<p>
				This page lists the information needed by the selected pit.
				Some of the information applies to the Peach environment;
				two examples are the Peach Installation Directory and the Pit Library Path.
				Other information is pit specific,
				such as port addresses for a network protocol or source files for a file format.
			</p>

			<Row className="margin-bottom-10">
				<Col xs={6}>
					{pristine && isSaved &&
						<span className="green">
							Saved successfully.
						</span>
					}
					{pristine && invalid &&
						<span className="red">
							There are required variables that must be configured.
						</span>
					}
					{dirty && invalid &&
						<span className="red">
							You have some validation issues to correct.
						</span>
					}
				</Col>
				<Col xs={6} className="text-right">
					<ButtonToolbar>
						<Button bsStyle='danger' bsSize='xs' 
							disabled={pristine || invalid || submitting}
							onClick={handleSubmit(this.onSave)}>
							<Icon name='save' />&nbsp; Save
						</Button>
						<Button bsStyle='info' bsSize='xs'
							disabled={submitting}
							onClick={this.onAdd}>
							Add Variable &nbsp; <Icon name='plus' />
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
			{statuses.pit === 'success' &&
				<Row>
					<Col xs={12}>
						<Accordion>
							{pairs.map((item, index) =>
								<PitDefines key={index} 
									param={item.param} 
									fields={item.fields} />
							)}
						</Accordion>
					</Col>
				</Row>
			}
		</div>
	}

	onSave = (data: FormData) => {
		const { pit, dispatch } = this.props;
		dispatch(saveDefines(pit, data));
	}

	onAdd = () => {
	}
}

export default Variables;
