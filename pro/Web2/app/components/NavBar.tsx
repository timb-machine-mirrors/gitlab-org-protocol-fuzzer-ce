import React = require('react');
import { Component } from 'react';

import { R } from '../containers';
import Link from './Link';

class NavBar extends Component<{}, {}> {
	render() {
		return <div className="navbar">
			<div className="navbar-container">
				<div className="navbar-header">
					<Link to={R.Root} options={{ reload: true }}>
						<img src="/img/peachlogo.png" />
					</Link>
				</div>
			</div>
		</div>
	}
}

export default NavBar;
