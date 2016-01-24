import React = require('react');
import Icon = require('react-fa');
import classNames = require('classnames');
import { Component, Props, ReactNode, MouseEvent } from 'react';
import { Accordion, Alert, Panel, Button, ButtonGroup, Input } from 'react-bootstrap';
import { FieldProp } from 'redux-form';

import PitMonitor from './PitMonitor';
import FoldingPanel from './FoldingPanel';
import { Pit, Agent, Monitor, Parameter } from '../models/Pit';
import { validationState } from '../utils';
import AddMonitorModal from './AddMonitorModal';
import { findMonitorMetadata, mapMonitorToView } from '../redux/modules/Pit';

interface PitAgentProps extends Props<PitAgent> {
	pit: Pit;
	fields: any;
	index: number;
}

interface PitAgentState {
	showModal?: boolean;
}

class PitAgent extends Component<PitAgentProps, PitAgentState> {
	constructor(props, context) {
		super(props, context);
		this.state = {
			showModal: false
		};
	}

	render() {
		const { pit, fields, index } = this.props;
		const { monitors } = pit.metadata;
		const { showModal } = this.state;
		const agent = fields[index];

		return <FoldingPanel header={this.renderHeader}>
			<Input type='text'
				label='Name'
				labelClassName='col-sm-3' 
				wrapperClassName='col-sm-6'
				addonBefore={this.renderBefore()} 
				hasFeedback 
				help={agent.name.error} 
				bsStyle={validationState(agent.name)} 
				{...agent.name} />

			<Input type='text'
				label='Location'
				labelClassName='col-sm-3' 
				wrapperClassName='col-sm-6'
				addonBefore={this.renderBefore()} 
				hasFeedback 
				placeholder='local://'
				help={agent.location.error} 
				bsStyle={validationState(agent.location)} 
				{...agent.location} />

			{!agent.monitors.length &&
				<Alert bsStyle='warning'>
					<strong>Warning!</strong> &nbsp; At least one monitor is advised.
				</Alert>
			}

			<div className="clearfix"
				style={{ marginBottom: 10 }}>
				<div className="pull-right">
					<Button bsStyle='info' bsSize='xs'
						onClick={this.onAddMonitor}>
						Add Monitor &nbsp; <Icon name="plus" />
					</Button>
				</div>
			</div>
			<Accordion>
				{agent.monitors.map((monitor, index) => 
					<PitMonitor key={index}
						monitor={this.getMonitorMetadata(monitor)}
						fields={agent.monitors} 
						index={index} />
				)}
			</Accordion>

			{showModal &&
				<AddMonitorModal monitors={monitors}
					onComplete={this.onAddMonitorComplete} />
			}
		</FoldingPanel>
	}

	renderBefore() {
		return <Icon name='question-circle' />
	}

	renderHeader = (isOpen) => {
		const { fields, index } = this.props;
		const agent = fields[index];
		const url = agent.location.value || 'local://';
		const name = agent.name.value ? `(${agent.name.value})` : '';
		const header = `${url} ${name}`;
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

	getMonitorMetadata(monitor): Parameter {
		const { pit } = this.props;
		const monitorClass = monitor.monitorClass.value;
		return findMonitorMetadata(pit, monitorClass);
	}

	onAddMonitor = () => {
		this.setState({ showModal: true });
	}

	onAddMonitorComplete = (metadata: Parameter) => {
		this.setState({ showModal: false });
		if (!metadata) {
			return;
		}

		const { fields, index } = this.props;
		const monitor: Monitor = {
			name: '',
			description: '',
			monitorClass: metadata.key,
			map: []
		};
		const view = mapMonitorToView(monitor, metadata);
		const subset = [
			'monitorClass',
			'name',
			'params[]',
			'groups[].params[]'
		];
		fields[index].monitors.addField(view, undefined, subset);
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

export default PitAgent;
