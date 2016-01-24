// Type definitions for react-fa v4.0.0

declare module "react-fa" {
	import { ComponentClass, Props } from 'react';

	interface IconProps extends Props<Icon> {
		name: string;
		className?: string;
		size?: string;
		spin?: boolean;
		rotate?: string;
		flip?: string;
		fixedWidth?: boolean;
		pulse?: boolean;
		stack?: string;
		inverse?: boolean;
	}

	interface Icon extends ComponentClass<IconProps> { }
	const Icon: Icon;

	export = Icon;
}
