import React = require('react');
import superagent = require('superagent');
import { Component, Props } from 'react';
import { reduxForm, ReduxFormProps, FormData, FieldProp } from 'redux-form';
import { Dispatch } from 'redux';
import { Button, Input, Label, Modal } from 'react-bootstrap';

import { LibraryState } from '../models/Library';
import { Pit, PitCopy } from '../models/Pit';
import { validationState } from '../utils';

interface NewModalProps extends Props<NewPitModal> {
	pit: Pit;
	onComplete: (pit: Pit) => void;
	initialValues: any;
	// injected
	formProps?: ReduxFormProps;
	library?: LibraryState;
}

const validate = values => {
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
		return (
			<Modal show={true}
				onHide={this.onCancel}
				autoFocus>
				<form className="form-horizontal">
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
							type="text"
							label="Name"
							labelClassName="col-sm-2"
							wrapperClassName="col-sm-8"
							hasFeedback
							bsStyle={validationState(name)}
							help={name.error}
							autoFocus
						/>
						<Input {...description}
							type="text"
							label="Description"
							labelClassName="col-sm-2"
							wrapperClassName="col-sm-8"
							hasFeedback
							bsStyle={validationState(description)}
							help={description.error} 
						/>
					</Modal.Body>
					<Modal.Footer>
						{error &&
							<h4 className="pull-left">
								<Label bsStyle="danger">
									{error}
								</Label>
							</h4>
						}
						<Button bsStyle="default"
							onClick={this.onCancel}>
							Cancel
						</Button>
						<Button type="submit"
							bsStyle="primary"
							disabled={submitting}
							onClick={handleSubmit(this.onSubmit)}>
							Submit
						</Button>
					</Modal.Footer>
				</form>
			</Modal>
		)
	}

	onCancel = () => {
		this.props.onComplete(null);
	}

	onSubmit = (data: FormData) => {
		const { name, description } = data;
		const request: PitCopy = {
			libraryUrl: this.props.library.libraryUrl,
			pitUrl: this.props.pit.pitUrl,
			name: name,
			description: description
		};

		return new Promise((resolve, reject) => {
			superagent.post('/p/pits')
				.type('json')
				.accept('json')
				.send(request)
				.end((err, res) => {
					if (err) {
						reject({_error: err.toString()});
					} else {
						resolve();
						this.props.onComplete(res.body);
					}
				})
			;
		});
	}
}

export default NewPitModal;
