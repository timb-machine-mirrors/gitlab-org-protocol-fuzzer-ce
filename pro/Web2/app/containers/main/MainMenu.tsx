import React = require('react');
import { Component } from 'react';

import { Menu, MenuItem } from '../../components/Menu';
import { R } from '../../routes';

class MainMenu extends Component<{}, {}> {
	render() {
		return <Menu>
			<MenuItem to={R.Root} />
			<MenuItem to={R.Library} />
			<MenuItem to={R.Jobs} />
		</Menu>
	}
}

export default MainMenu;
