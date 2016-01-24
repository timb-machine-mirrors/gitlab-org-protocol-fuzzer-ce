import React = require('react');
import { Component } from 'react';
import { Button, Row, Col } from 'react-bootstrap';

class Intro extends Component<{}, {}> {
	render() {
		return <Row>
			<Col xs={12}>
				<p>
					This wizard helps set up a fuzzing definition (Pit) from the Peach Library.
					Pits contain the information needed to fuzz a test target.The information
					supplied via the wizard provides information specific to the test
					environment.
				</p>
				<p>
					After completing this wizard, the Pit will be fully configured and ready to use.
				</p>
				<p>
					This wizard will help you set up the following items:
				</p>
				<ul>
					<li>Pit variables</li>
					<li>Fault detection</li>
					<li>Data collection</li>
					<li>Environment automation</li>
					<li>Validating the Pit configuration with a test</li>
					<li>Saving the work for later use</li>
				</ul>
			</Col>

			<div className="wizard-actions">
				<Button bsStyle="success" bsSize="sm">
					Begin &rarr;
				</Button>
			</div>
		</Row >
	}
}

export default Intro;
