import React = require('react');
import { Component } from 'react';

class Intro extends Component<{}, {}> {
	render() {
		return <div>
			<p>
				Peach monitors the test target while fuzzing to detect unknown or
				undesireable behaviors called faults.For example, a fault could
				be a program crash.Another fault could be a remote device that
				becomes unavailable.
			</p>
			<p>
				This portion of the wizard sets up the Pit to define fault detection
				that is appropriate for the target you are fuzzing.
			</p>
			<p>
				The Pit Fault Detection section requires the following information:
			</p>
			<ul>
				<li>
					Where to to perform fault detection: on the local machine or on a
					remote machine.
				</li>
				<li>
					If fault detection is remote, what is the operating system and the
					name (hostname or IP address) of the remote machine?
				</li>
				<li>
					If a process or a service is being tested and monitored for
					faults, the wizard will request information about starting the
					process or service.
				</li>
			</ul>
		</div>
	}
}

export default Intro;
