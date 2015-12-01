 /// <reference path="../reference.ts" />

namespace Peach {
	export namespace ParameterType {
		export const String = 'string';
		export const Hex = 'hex';
		export const Range = 'range';
		export const Ipv4 = 'ipv4';
		export const Ipv6 = 'ipv6';
		export const Hwaddr = 'hwaddr';
		export const Iface = 'iface';
		export const Enum = 'enum';
		export const Bool = 'bool';
		export const User = 'user';
		export const System = 'system';
		export const Call = 'call';
		export const Group = 'group';
		export const Space = 'space';
		export const Monitor = 'monitor';
	}

	export interface IParameter {
		key?: string;
		value?: any;
		name: string;
		type?: string;
		items?: IParameter[];
		options?: string[];
		defaultValue?: string;
		description?: string;
		min?: number;
		max?: number;
		optional?: boolean;
	}

	export interface IAgent {
		name: string;
		agentUrl: string;
		monitors: IMonitor[];
	}

	export interface IMonitor {
		monitorClass: string;
		name?: string;
		map: IParameter[];
		description: string;

		// for use by the wizard
		path?: number[];
	}

	export interface IPitMetadata {
		defines: IParameter[];
		monitors: IParameter[];
	}
	
	export interface IPit {
		id: string;
		pitUrl: string;
		name: string;
		description: string;
		tags: ITag[];

		// details, not available from collection at /p/pits
		config: IParameter[];
		agents: IAgent[];
		metadata?: IPitMetadata;
	}

	export interface IPitCopy {
		// Url of the destination Pit Library
		libraryUrl: string;
		pitUrl: string;
		name: string;
		description: string;
	}
}
