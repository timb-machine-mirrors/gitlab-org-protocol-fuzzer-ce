import React = require('react');
import Icon = require('react-fa');
import classNames = require('classnames');
import { Component, Props, ReactNode, MouseEvent } from 'react';

interface FoldingPanelProps extends Props<FoldingPanel> {
	defaultCollapsed?: boolean;
	header: (isOpen: boolean) => ReactNode;
}

interface FoldingPanelState {
	isOpen: boolean;
}

class FoldingPanel extends Component<FoldingPanelProps, FoldingPanelState> {
	constructor(props: FoldingPanelProps, context) {
		super(props, context);
		this.state = {
			isOpen: !props.defaultCollapsed
		};
	}

	render() {
		const { isOpen } = this.state;
		const { header, children } = this.props;
		const collapseClass = classNames('panel-collapse', 'collapse', { 'in': isOpen });

		return <div className='panel panel-default'>
			<div className='panel-heading'>
				<h4 className='panel-title'>
					<a href=''
						className='accordion-toggle'
						onClick={this.onClick}>
						{header(isOpen)}
					</a>
				</h4>
			</div>
			<div className={collapseClass}>
				<div className='panel-body'>
					{children}
				</div>
			</div>
		</div>;
	}

	onClick = (event: MouseEvent) => {
		event.preventDefault();
		const { isOpen } = this.state;
		this.setState({ isOpen: !isOpen });
	};
}

export default FoldingPanel;
