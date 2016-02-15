import { MakeLowerEnum } from '../utils';

export const ParameterType = {
	String: '',
	Hex: '',
	Range: '',
	Ipv4: '',
	Ipv6: '',
	Hwaddr: '',
	Iface: '',
	Enum: '',
	Bool: '',
	User: '',
	System: '',
	Call: '',
	Group: '',
	Space: '',
	Monitor: ''
};
MakeLowerEnum(ParameterType);

export interface Parameter {
	key?: string;
	value?: any;
	name?: string;
	type?: string;
	items?: Parameter[];
	options?: string[];
	defaultValue?: string;
	description?: string;
	min?: number;
	max?: number;
	optional?: boolean;
	collapsed?: boolean;
}

export interface Agent {
	name: string;
	agentUrl: string;
	monitors: Monitor[];
}

export interface Monitor {
	monitorClass: string;
	name?: string;
	map: Parameter[];
	description?: string;

	// for use by the wizard
	path?: number[];
}

export interface PitMetadata {
	defines: Parameter[];
	monitors: Parameter[];
}

export interface Tag {
	name: string;
	values: string[];
}

export interface Pit {
	id: string;
	pitUrl: string;
	name: string;
	description?: string;
	tags?: Tag[];

	// details, not available from collection at /p/pits
	config: Parameter[];
	agents: Agent[];
	metadata?: PitMetadata;

	// only used by client-side
	definesView?: Parameter[];
	isConfigured?: boolean;
	hasMonitors?: boolean;
}

export interface PitCopy {
	// Url of the destination Pit Library
	libraryUrl: string;
	pitUrl: string;
	name: string;
	description: string;
}

export interface ParamsData {
	params: string[];
}

export interface DefinesFormData {
	groups: ParamsData[];
}

export interface MonitorData {
	name: string;
	monitorClass: string;
	groups: ParamsData[];
	params: string[];
}

export interface AgentData {
	name: string;
	location: string;
	monitors: MonitorData[];
}

export interface AgentsFormData {
	agents: AgentData[];
}
