import React = require('react');
import { Component, Props, MouseEvent } from 'react';
import { Dispatch } from 'redux';
import { connect } from 'redux-await';
import { Panel } from 'react-bootstrap';
import { actions } from 'redux-router5';

import { injectRouter, RouterContext } from '../../models/Router';
import { LibraryState, Category } from '../../models/Library';
import { Pit } from '../../models/Pit';
import { fetchLibrary } from '../../redux/modules/Library';
import { R } from '../../routes';
import NewPitModal from '../../components/NewPitModal';
import FoldingPanel from '../../components/FoldingPanel';

interface NewPitValues {
	name: string;
	description: string;
}

interface ModalState {
	showModal?: boolean;
	newPitValues?: NewPitValues;
	pit?: Pit;
}

interface LibraryProps extends Props<Library> {
	// injected
	library?: LibraryState;
	dispatch?: Dispatch;
	statuses?: any;
}

@connect(state => ({ library: state.library }))
@injectRouter
class Library extends Component<LibraryProps, ModalState> {
	context: RouterContext;

	constructor(props, context) {
		super(props, context);
		this.state = {
			showModal: false
		};
	}

	componentDidMount() {
		this.props.dispatch(fetchLibrary());
	}

	render() {
		const { pits, configurations } = this.props.library;
		return <div>
			<p>
				Welcome to the Peach Pit library. This page consists of two main parts:
			</p>

			<dl className="dl-horizontal">
				<dt>
					Pits
				</dt>
				<dd>
					All Peach Pits (test modules) that are present on your system
				</dd>

				<dt>
					Configurations
				</dt>
				<dd>
					Saved pit configurations
				</dd>
			</dl>

			<div className="page-header">
				<h3>Pits</h3>
			</div>
			<p>
				Peach Pits allow testing of a data format or a network protocol against a variety of targets.
			</p>

			{this.renderSection(pits, true)}

			<div className="page-header">
				<h3>Configurations</h3>
			</div>
			<p>
				The Configurations section contains existing Peach Pit configurations. 
				Selecting an existing configuration allows editing the configuration and starting a new fuzzing job.
			</p>

			{this.renderSection(configurations, false)}

			{this.state.showModal &&
				<NewPitModal
					pit={this.state.pit}
					onComplete={this.onNewPitComplete}
					initialValues={this.state.newPitValues} 
				/>
			}
		</div>
	}

	renderSection(data: Category[], isLocked: boolean) {
		const { statuses } = this.props;
		const isFetching = statuses.library === 'pending';
		return <div>
			{isFetching && data.length === 0 &&
				<h4>Loading...</h4>
			}

			{data.length > 0 && 
				<div style={{ opacity: isFetching ? 0.5 : 1 }}>
					{data.map((item, index) => this.renderCategory(item, index, isLocked))}
				</div>
			}
		</div>
	}

	renderCategory(category: Category, index: number, isLocked: boolean) {
		return <FoldingPanel key={index}
			header={() => category.name}>
			<ul className="list-inline library">
				{category.pits.map((x, y) => this.renderLink(x, y, isLocked))}
			</ul>
		</FoldingPanel>
	}

	renderLink(pit: Pit, index: number, isLocked: boolean) {
		const params = { pit: pit.id };
		const { router } = this.context;
		const href = router.buildUrl(R.Pit.name, params);

		const onClick = (evt: MouseEvent) => {
			evt.preventDefault();
			if (isLocked) {
				this.openNewPitModal(pit);
			} else {
				this.navigateToPit(pit);
			}
		}

		return <li key={index}>
			<a href={href} onClick={onClick}>
				{pit.name}
			</a>
		</li>
	}

	openNewPitModal(pit: Pit) {
		this.setState({
			showModal: true,
			pit: pit,
			newPitValues: {
				name: pit.name,
				description: pit.description
			}
		});
	}

	onNewPitComplete = (pit: Pit) => {
		this.setState({ showModal: false });
		
		if (pit) {
			this.navigateToPit(pit);
		}
	}

	navigateToPit(pit: Pit) {
		const to = R.Pit.name;
		const params = { pit: pit.id };
		const { dispatch } = this.props;
		dispatch(actions.navigateTo(to, params));
	}
}

export default Library;
