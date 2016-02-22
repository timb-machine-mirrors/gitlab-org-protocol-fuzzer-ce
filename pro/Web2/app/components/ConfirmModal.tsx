import React = require('react');
import { Component, Props, ReactNode } from 'react';
import { Button, Modal } from 'react-bootstrap';

interface ConfirmModalProps extends Props<ConfirmModal> {
	title?: ReactNode;
	body?: ReactNode;
	cancelPrompt?: ReactNode;
	submitPrompt?: ReactNode;
	onComplete: (result: boolean) => void;
}

class ConfirmModal extends Component<ConfirmModalProps, {}> {
	render() {
		const { title, body, submitPrompt, cancelPrompt } = this.props;

		return <Modal show={true}
			onHide={this.onCancel}>
			<Modal.Header closeButton>
				<Modal.Title>
					{title || 'Confirmation'}
				</Modal.Title>
			</Modal.Header>
			<Modal.Body>
				{body || 'Are you sure? This action is irreversible.'}
			</Modal.Body>
			<Modal.Footer>
				<Button bsStyle='default'
					onClick={this.onCancel}>
					{cancelPrompt || 'Cancel'}
				</Button>
				<Button type='submit'
					bsStyle='primary'
					onClick={this.onOK}>
					{submitPrompt || 'OK'}
				</Button>
			</Modal.Footer>
		</Modal>;
	}

	onOK = () => {
		this.props.onComplete(true);
	};

	onCancel = () => {
		this.props.onComplete(false);
	};
}

export default ConfirmModal;
