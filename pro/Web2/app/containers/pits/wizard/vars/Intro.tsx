import React = require('react');
import { Component } from 'react';

class Intro extends Component<{}, {}> {
	render() {
		return <div>
			<p>
				The Pit Set Variables wizard guides you through those settings required
				to run the Pit.The settings are specific to the selected Pit, as the
				setup information for each pit can vary.
				For some settings, the wizard offers guidance on how to locate the needed
				information.
			</p>
			<p>
				For example, network-related Pits typically need network addresses such as the
				IP address or the MAC address of the target.
			</p>
			<p>
				The configuration settings for each Pit are described in the Pit Library
				Documentation.This document is included in the Pits zip archive.The
				Pit_Library.pdf file resides in the Pit Library directory.
			</p>
		</div>
	}
}

export default Intro;
