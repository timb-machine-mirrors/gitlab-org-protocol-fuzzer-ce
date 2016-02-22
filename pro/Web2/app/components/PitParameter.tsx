import React = require('react');
import Icon = require('react-fa');
import { Component, Props } from 'react';
import { FieldProp } from 'redux-form';
import { Input } from 'react-bootstrap';

import { Parameter, ParameterType } from '../models/Pit';
import { validationState } from '../utils';

interface ParameterOptions {
	showLabel?: boolean;
	labelClassName?: string;
	wrapperClassName?: string;
}

interface ParameterProps {
	param: Parameter;
	field: FieldProp;
	options?: ParameterOptions;
}

class SelectParam extends Component<{}, {}> {
	render() {
		return (
			<div className='input-group'>
				<span className='input-group-addon'
					uib-tooltip='{{param.description}}'
					tooltip-placement='above'
					tooltip-append-to-body='true'>
					<i className='fa fa-question-circle'></i>
				</span>
				<input type='select'>
					<option value=''></option>
				</input>
			</div>
		);
	}
}

class ComboParam extends Component<{}, {}> {
	render() {
		return <div />;
	}
}

interface StringParamProps extends ParameterProps, Props<StringParam> {
}

class StringParam extends Component<StringParamProps, {}> {
	render() {
		const { param, field, options } = this.props;
		const showLabel = options && options.showLabel;
		const isReadonly = param.type === ParameterType.System;
		const isRequired = false;

		return <Input type='text'
			label={showLabel ? param.name : undefined}
			labelClassName={options && options.labelClassName}
			wrapperClassName={options && options.wrapperClassName}
			addonBefore={this.renderBefore()}
			readOnly={isReadonly}
			placeholder={param.defaultValue}
			hasFeedback
			help={field.error}
			bsStyle={validationState(field)}
			{...field}
		/>;
	}

	renderBefore() {
		return <Icon name='question-circle' />;
	}
}

interface PitParameterProps extends ParameterProps, Props<PitParameter> {
}

class PitParameter extends Component<PitParameterProps, {}> {
	render() {
		const { param, field, options } = this.props;
		switch (param.type) {
			case ParameterType.Space:
				return <div />;
			case ParameterType.Enum:
			case ParameterType.Bool:
			case ParameterType.Call:
			// return <SelectParam />;
			case ParameterType.Hwaddr:
			case ParameterType.Iface:
			case ParameterType.Ipv4:
			case ParameterType.Ipv6:
				// return <ComboParam />;
			default:
				return <StringParam param={param} field={field} options={options} />;
		}
	}
}

export default PitParameter;
