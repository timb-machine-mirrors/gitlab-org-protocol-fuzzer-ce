import React = require('react');

let DevTools;

declare var __DEV__;
if (__DEV__) {
	const devtools = require('redux-devtools');
	const LogMonitor = require('redux-devtools-log-monitor').default;
	const DockMonitor = require('redux-devtools-dock-monitor').default;

	DevTools = devtools.createDevTools(
		<DockMonitor
			defaultIsVisible={false}
			toggleVisibilityKey='ctrl-h'
			changePositionKey='ctrl-q'>
			<LogMonitor theme='tomorrow' />
		</DockMonitor>
	);
} else {
	class DevToolsMock extends React.Component<{}, {}> {
		render() {
			return (<div/>);
		}

		static instrument() {
			return next => action => {
				return next(action);
			};
		}
	}

	DevTools = DevToolsMock;
}

export default DevTools;
