import React = require('react');
import { Component, Props } from 'react';

import { Menu, MenuItem } from '../../components/Menu';
import { R } from '../../containers';

class PitsMenu extends Component<Props<PitsMenu>, {}> {
	render() {
		return <Menu>
			<MenuItem to={R.Pit} />
			<MenuItem to={R.PitWizard}>
				<MenuItem to={R.PitWizard}  />
				<MenuItem to={R.PitWizardVars} />
				<MenuItem to={R.PitWizardFault} />
				<MenuItem to={R.PitWizardData} />
				<MenuItem to={R.PitWizardAuto} />
				<MenuItem to={R.PitWizardTest} />
			</MenuItem>
			<MenuItem to={R.PitAdvanced}>
				<MenuItem to={R.PitAdvancedVariables} />
				<MenuItem to={R.PitAdvancedMonitoring} />
				<MenuItem to={R.PitAdvancedTuning} />
				<MenuItem to={R.PitAdvancedTest} />
			</MenuItem>
		</Menu>
	}
}

export default PitsMenu;
