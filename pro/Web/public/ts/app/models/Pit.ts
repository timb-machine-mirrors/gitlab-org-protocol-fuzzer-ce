 /// <reference path="../reference.ts" />

namespace Peach {
	export var ParameterType = {
		String  : '',
		Hex     : '',
		Range   : '',
		Ipv4    : '',
		Ipv6    : '',
		Hwaddr  : '',
		Iface   : '',
		Enum    : '',
		Bool    : '',
		User    : '',
		System  : '',
		Call    : '',
		Group   : '',
		Space   : '',
		Monitor : ''
	};
	MakeLowerEnum(ParameterType);

	export interface IParameter {
		key?: string;
		value?: any;
		name?: string;
		type?: string;
		items?: IParameter[];
		options?: string[];
		defaultValue?: string;
		description?: string;
		min?: number;
		max?: number;
		optional?: boolean;
		collapsed?: boolean;
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
		description?: string;

		// for use by the wizard
		path?: number[];

		// only used by client-side
		view?: IParameter;
	}

	export interface IPitMetadata {
		defines: IParameter[];
		monitors: IParameter[];
	}
	
	export interface IPit {
		id: string;
		pitUrl: string;
		name: string;
		description?: string;
		tags?: ITag[];

		// details, not available from collection at /p/pits
		config: IParameter[];
		agents: IAgent[];
		metadata?: IPitMetadata;
	
		// only used by client-side
		definesView?: IParameter[];
	}

	export interface IPitCopy {
		// Url of the destination Pit Library
		libraryUrl: string;
		pitUrl: string;
		name: string;
		description: string;
	}
}
