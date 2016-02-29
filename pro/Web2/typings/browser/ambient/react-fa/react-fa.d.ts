// Compiled using typings@0.6.8
// Source: https://raw.githubusercontent.com/flaub/typescript-definitions/master/react-fa/react-fa.d.ts
// Type definitions for react-fa v4.0.0
// Project: https://github.com/andreypopp/react-fa
// Definitions by: Frank Laub <https://github.com/flaub>

declare module "react-fa" {
	import { ComponentClass, CSSProperties, DOMAttributes, Props } from 'react';

	interface IconProps extends DOMAttributes, Props<Icon> {
		name: string;
		className?: string;
		style?: CSSProperties;
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