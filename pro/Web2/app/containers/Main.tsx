import React = require('react');
import { Component, MouseEvent, Props } from 'react';
import Icon = require('react-fa');
import classNames = require('classnames');
import { Alert, OverlayTrigger, Tooltip, Grid } from 'react-bootstrap';
import { connect } from 'react-redux';
import { bindActionCreators, Dispatch } from 'redux';

import { R, RouteSpec } from '../routes';
import { Actions } from '../redux/modules/Error';
import { injectRouter, RouterContext } from '../models/Router';
import Breadcrumbs from '../components/Breadcrumbs';
import BreadcrumbLeaf from '../components/BreadcrumbLeaf';
import Segment from '../components/Segment';
import NavBar from '../components/NavBar';

interface MainProps extends Props<Main> {
	// injected
	error?: string;
	actions?: Actions;
}

interface MainState {
	isSidebarCollapsed?: boolean;
}

@connect(
	state => ({ error: state.error }),
	dispatch => ({ actions: new Actions(dispatch) })
)
@injectRouter
class Main extends Component<MainProps, MainState> {
	context: RouterContext;

	constructor(props?: MainProps, context?: any) {
		super(props, context);
		this.state = {
			isSidebarCollapsed: false
		};

		this.context.router.addListener(() => {
			this.props.actions.clear();
		});
	}

	render() {
		return (
			<div>
				<NavBar />
				{this.renderMainContainer()}
			</div>
		)
	}

	renderMainContainer() {
		const sidebarClass = classNames('sidebar', {
			'menu-min': this.state.isSidebarCollapsed
		});

		const sidebarCollapseIcon = this.state.isSidebarCollapsed ? 
			'angle-double-right' : 
			'angle-double-left';

		return (
			<div className="main-container">
				<div className="main-container-inner">
					<div className={sidebarClass}>
						<div className="sidebar-shortcuts">
							<div className="sidebar-shortcuts-large">
								{this.renderShortcut('home', R.Root, 'Home', 'home') }
								{this.renderShortcut('libs', R.Library, 'Library', 'book') }
								{this.renderShortcut('jobs', R.Jobs, 'Jobs', 'history') }
								{this.renderShortcut('help', R.Docs, 'Help', 'question', true) }
							</div>
						</div>

						<Segment part={0} />

						<div 
							className="sidebar-collapse" 
							onClick={this.onToggleSidebar}>
							<Icon name={sidebarCollapseIcon} />
						</div>
					</div>

					{this.renderMainContent()}
				</div>
			</div>
		)
	}

	renderShortcut(id: string, to: RouteSpec, tooltip: string, icon: string, isExternal: boolean = false) {
		const overlay = (<Tooltip id={`tt-${id}`}>{tooltip}</Tooltip>);
		const { router } = this.context;
		return (
			// <OverlayTrigger placement="top" overlay={overlay}>
				<button 
					className="btn"
					href={to.path}
					onClick={() => {
						if (isExternal) {
							open(to.path, '_blank');
						} else {
							router.navigate(to.name);
						}
					}}>
					<Icon name={icon} />
				</button>
			// </OverlayTrigger>
		)
	}

	renderMainContent() {
		const { error } = this.props;
		return (
			<div className="main-content">
				<div className="page-content">
					<Breadcrumbs />

					<div className="page-header">
						<BreadcrumbLeaf />
					</div>
	
					{error &&
						<Alert bsStyle="danger">
							<strong>Error!</strong>
							&nbsp; {error}
						</Alert>
					}

					<Grid fluid>
						<Segment part={1} />
					</Grid>

				</div>
			</div>
		)
	}

	onToggleSidebar = (evt: MouseEvent) => {
		this.setState({ 
			isSidebarCollapsed: !this.state.isSidebarCollapsed
		});
	}
}

export default Main;
