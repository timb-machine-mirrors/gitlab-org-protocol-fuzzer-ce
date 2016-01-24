import React = require('react');
import Icon = require('react-fa');
import { Component, Props, FormEvent } from 'react';
import { Button, Input, Accordion, Modal, Panel, ListGroup, ListGroupItem } from 'react-bootstrap';

import { Parameter } from '../models/Pit';
import FoldingPanel from './FoldingPanel';

interface AddMonitorModalProps extends Props<AddMonitorModal> {
	monitors: Parameter[];
	onComplete: (monitor: Parameter) => void;
}

interface AddMonitorModalState {
	search?: string;
	selected?: Parameter;
}

class AddMonitorModal extends Component<AddMonitorModalProps, AddMonitorModalState> {
	constructor(props: AddMonitorModalProps, context) {
		super(props, context);
		this.state = {
			search: '',
			selected: null
		};
	}

	render() {
		const { monitors } = this.props;
		const { selected } = this.state;

		return <Modal show={true} 
			onHide={this.onCancel}
			autoFocus>
			<Modal.Header closeButton>
				<Modal.Title>
					Add Monitor
				</Modal.Title>
			</Modal.Header>
			<Modal.Body>
				<Input type='text'
					placeholder='Search for monitor...'
					buttonAfter={this.renderAfter()}
					onChange={this.onChange}
					autoFocus />

				<div className="peach-add-monitor">
					{this.filterGroups().map((group, i) => 
						<FoldingPanel key={i} 
							header={() => group.name}>
							<ListGroup>
								{this.filterMonitors(group).map((monitor, j) => 
									<ListGroupItem key={j}
										header={monitor.name}
										onClick={() => this.onSelect(monitor)}>
										{monitor.description}
									</ListGroupItem>
								)}
							</ListGroup>
						</FoldingPanel>
					)}
				</div>

			</Modal.Body>
			<Modal.Footer>
				<Button bsStyle="default"
					onClick={this.onCancel}>
					Cancel
				</Button>
				<Button type="submit"
					bsStyle="primary"
					disabled={_.isNull(selected)}
					onClick={this.onOK}>
					OK
				</Button>
			</Modal.Footer>
		</Modal>
	}

	renderAfter() {
		return <Button bsStyle='default'>
			<Icon name='search' />
		</Button>
	}

	filterGroups() {
		const { monitors } = this.props;
		return _.reject(monitors, x => 
			_.isEmpty(this.filterMonitors(x))
		);
	}

	filterMonitors(group: Parameter) {
		const { search } = this.state;
		return _.filter(group.items, x => 
			x.name.toLowerCase().indexOf(search) !== -1
		);
	}

	onOK = () => {
		this.props.onComplete(this.state.selected);
	}

	onCancel = () => {
		this.props.onComplete(null);
	}

	onSelect = (monitor: Parameter) => {
		this.setState({ selected: monitor });
	}

	onChange = (event: FormEvent) => {
		const target = event.target as HTMLInputElement;
		const search = target.value.toLowerCase();
		this.setState({ search })
	}
}

export default AddMonitorModal;
