import React = require('react');
import Icon = require('react-fa');
import { Component, Props, MouseEvent } from 'react';
import { Accordion, Panel, Input, Button, ButtonGroup } from 'react-bootstrap';
import { FieldProp } from 'redux-form';

import FoldingPanel from './FoldingPanel';
import PitParameterTree from './PitParameterTree';
import { Parameter } from '../models/Pit';
import { validationState } from '../utils';

interface PitMonitorProps extends Props<PitMonitor> {
	monitor: Parameter;
	fields: any;
	index: number;
}

class PitMonitor extends Component<PitMonitorProps, {}> {
	render() {
		const { fields, monitor, index } = this.props;
		const self = fields[index];

		return <FoldingPanel header={this.renderHeader}>
			<Input type='text'
				label='Name'
				labelClassName='col-sm-4' 
				wrapperClassName='col-sm-6'
				addonBefore={this.renderBefore()} 
				hasFeedback 
				help={self.name.error} 
				bsStyle={validationState(self.name)} 
				{...self.name} />

				{monitor.items.map((param, index) => 
					<PitParameterTree key={index} 
						param={param}
						fields={self}
						index={index} />
				)}
		</FoldingPanel>
	}

	renderHeader = (isOpen: boolean) => {
		const { fields, monitor, index } = this.props;
		const self = fields[index];

		const klass = self.monitorClass.value;
		const name = self.name.value ? `(${self.name.value})` : '';
		const header = `${klass} ${name}`;
		const icon = isOpen ? 'chevron-down' : 'chevron-right';

		return <span>
			<Icon name={icon} /> &nbsp; {header}
			<ButtonGroup bsSize='xs' className='pull-right'>
				<Button bsStyle='info' bsSize='xs'
					onClick={this.onMoveUp}
					disabled={index === 0}
					uib-tooltip="Move Agent Up"
					tooltip-append-to-body="true">
					<Icon name="chevron-up" /> 
				</Button>
				<Button bsStyle='info' bsSize='xs'
					onClick={this.onMoveDown}
					disabled={index === (fields.length - 1)}
					uib-tooltip="Move Agent Down"
					tooltip-append-to-body="true">
					<Icon name="chevron-down" />
				</Button>
				<Button bsStyle='danger' bsSize='xs'
					onClick={this.onRemove}
					uib-tooltip="Remove Agent"
					tooltip-append-to-body="true">
					<Icon name="remove" />
				</Button>
			</ButtonGroup>
		</span>
	}

	renderBefore() {
		return <Icon name='question-circle' />
	}

	onMoveUp = (event: MouseEvent) => {
		event.stopPropagation();
		event.preventDefault();
		const { fields, index } = this.props;
		fields.swapFields(index, index - 1);
	}

	onMoveDown = (event: MouseEvent) => {
		event.stopPropagation();
		event.preventDefault();
		const { fields, index } = this.props;
		fields.swapFields(index, index + 1);
	}

	onRemove = (event: MouseEvent) => {
		event.stopPropagation();
		event.preventDefault();
		const { fields, index } = this.props;
		fields.removeField(index);
	}
}

export default PitMonitor;
