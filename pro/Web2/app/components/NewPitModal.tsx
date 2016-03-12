import React = require('react');
import { Component, Props } from 'react';
import { reduxForm, ReduxFormProps, FormData } from 'redux-form';
import { Dispatch } from 'redux';
import { Button, Input, Label, Modal } from 'react-bootstrap';

import { LibraryState } from '../models/Library';
import { Pit, PitCopy } from '../models/Pit';
import { validationState } from '../utils';
import { api } from '../services';

interface NewModalProps extends Props<NewPitModal> {
	pit: Pit;
	onComplete: (pit: Pit) => void;
	initialValues: any;
	// injected
	formProps?: ReduxFormProps;
	library?: LibraryState;
}

function validate(values) {
	const errors: any = {};
	if (!values.name) {
		errors.name = 'Required';
	}
	return errors;
}

@reduxForm({
	form: 'NewPit',
	fields: ['name', 'description'],
	propNamespace: 'formProps',
	validate
}, state => ({ library: state.library }))
class NewPitModal extends Component<NewModalProps, {}> {
	render() {
		const {
			fields: { name, description },
			error,
			handleSubmit,
			submitting
		} = this.props.formProps;
		return <Modal show={true}
			onHide={this.onCancel}
			autoFocus>
			<form className='form-horizontal'>
				<Modal.Header closeButton>
					<Modal.Title>
						New Pit Configuration
					</Modal.Title>
				</Modal.Header>
				<Modal.Body>
					<p>
						This will create a new configuration for the <code>{name.initialValue}</code> pit.
						You will then be able to edit the configuration and start a new fuzzing job.
					</p>
					<Input {...name}
						type='text'
						label='Name'
						labelClassName='col-sm-2'
						wrapperClassName='col-sm-8'
						hasFeedback
						bsStyle={validationState(name)}
						help={name.error}
						autoFocus
					/>
					<Input {...description}
						type='text'
						label='Description'
						labelClassName='col-sm-2'
						wrapperClassName='col-sm-8'
						hasFeedback
						bsStyle={validationState(description)}
						help={description.error}
					/>
					{error &&
						<h4>
							<Label bsStyle='danger'>
								{error}
							</Label>
						</h4>
					}
				</Modal.Body>
				<Modal.Footer>
					<Button bsStyle='default'
						onClick={this.onCancel}>
						Cancel
					</Button>
					<Button type='submit'
						bsStyle='primary'
						disabled={submitting}
						onClick={handleSubmit(this.onSubmit)}>
						Submit
					</Button>
				</Modal.Footer>
			</form>
		</Modal>;
	}

	onCancel = () => {
		this.props.onComplete(null);
	};

	onSubmit = (data: FormData) => {
		const { name, description } = data;
		const request: PitCopy = {
			pitUrl: this.props.pit.pitUrl,
			name: name,
			description: description
		};

		return new Promise((resolve, reject) => {
			api.createPit(request)
				.then(pit => {
					resolve();
					this.props.onComplete(pit);
				}, err => {
					if (err.response.status === 400) {
						reject({
							_error: `${name} already exists, please choose a new name.`
						});
					} else {
						reject({ _error: err.message });
					}
				})
			;
		});
	};
}

export default NewPitModal;
