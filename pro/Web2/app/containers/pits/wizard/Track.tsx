import React = require('react');
import classNames = require('classnames');
import { Component } from 'react';
import { Button, Panel } from 'react-bootstrap';

import Segment from '../../../components/Segment';

class Track extends Component<{}, {}> {
	render() {
		const steps = [
			classNames({ active: true, complete: true }),
			classNames({ active: true, complete: true }),
			classNames({ active: true })
		];

		return <Panel bsStyle='default'
			header={this.renderHeader()}>
			<ul className='wizard-steps'>
				<li className={steps[0]}>
					<span className='step'>1</span>
					<span className='title'>Introduction</span>
				</li>
				<li className={steps[1]}>
					<span className='step'>2</span>
					<span className='title'>Questions &amp; Answers</span>
				</li>
				<li className={steps[2]}>
					<span className='step'>3</span>
					<span className='title'>Review</span>
				</li>
			</ul>
			<hr />

			<Segment part={3} />
		</Panel>;
	}

	renderHeader() {
		const nextPrompt = 'Next';
		const backPrompt = 'Back';
		return <span>
			<span>
				<Button type='button'
					bsStyle='default'
					bsSize='sm'
					ng-click='vm.Back()'
					ng-disabled='!vm.CanMoveBack'>
					&larr; {backPrompt}
				</Button>
			</span>
			<span className='pull-right'>
				<Button type='submit'
					bsStyle='success'
					bsSize='sm'
					ng-click='vm.Next()'
					ng-disabled='!vm.CanMoveNext'>
					{nextPrompt} &rarr;
				</Button>
			</span>
		</span>;
	}
}

export default Track;
