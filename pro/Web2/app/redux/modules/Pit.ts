import { AWAIT_MARKER } from 'redux-await';
import { FormData } from 'redux-form';
import { fork, take, put, select } from 'redux-saga/effects';

import RootState from '../../models/Root';
import {
	Pit, Parameter, ParameterType, Monitor, Agent,
	DefinesFormData, AgentsFormData
} from '../../models/Pit';
import { MakeEnum, validationMessages } from '../../utils';
import { resetTest } from './PitTest';
import { api } from '../../services';

const types = {
	PIT_FETCH: '',
	PIT_SAVE: '',
};
MakeEnum(types);

const initial: Pit = {
	id: null,
	pitUrl: null,
	name: null,
	config: [],
	agents: [],
	definesView: [],
	isConfigured: false,
	hasMonitors: false
};

export default function reducer(state: Pit = initial, action): Pit {
	switch (action.type) {
		case types.PIT_FETCH:
		case types.PIT_SAVE:
			return onReceive(state, action);
		default:
			return state;
	}
}

export function* saga() {
	yield fork(watchSave);
	yield fork(watchFetch);
}

export function fetchPit(id: string) {
	return {
		type: types.PIT_FETCH,
		AWAIT_MARKER,
		payload: {
			pit: api.fetchPit(id)
		}
	};
}

export function saveDefines(pit: Pit, data: FormData) {
	const dto = _.pick(pit, 'id', 'pitUrl', 'name', 'agents') as Pit;
	dto.config = mapDefinesFromView(pit.definesView, data as DefinesFormData);
	return savePit(dto);
}

export function saveAgents(pit: Pit, data: FormData) {
	const dto = _.pick(pit, 'id', 'pitUrl', 'name', 'config') as Pit;
	dto.agents = mapAgentsFromView(pit, data as AgentsFormData);
	return savePit(dto);
}

function* watchSave() {
	while (true) {
		yield take(types.PIT_SAVE);
		yield put(resetTest());
	}
}

function* watchFetch() {
	while (true) {
		const { pit } = yield select<RootState>();
		const action = yield take(types.PIT_FETCH);
		if (pit.id !== action.payload.pit.id) {
			yield put(resetTest());
		}
	}
}

function savePit(pit: Pit) {
	return {
		type: types.PIT_SAVE,
		AWAIT_MARKER,
		payload: {
			pit: api.savePit(pit)
		}
	};
}

function onReceive(state: Pit, action): Pit {
	const pit: Pit = action.payload.pit;
	if (pit.metadata) {
		pit.definesView = mapDefinesToView(pit);
	}
	pit.isConfigured = _.every(pit.definesView, checkDefine);
	pit.hasMonitors = _.some(pit.agents, agent => agent.monitors.length > 0);
	return pit;
}

function mapDefinesToView(pit: Pit): Parameter[] {
	return _.map(pit.metadata.defines, g =>
		_.assign({}, g, {
			items: _.reduce(g.items, (result, param) => {
				const config = _.find(pit.config, { key: param.key });
				if (config && config.value) {
					result.push(_.assign({}, param, { value: config.value }));
				} else {
					result.push(param);
				}
				return result;
			}, [] as Parameter[])
		})
	);
}

function mapDefinesFromView(view: Parameter[], data: DefinesFormData): Parameter[] {
	const skip = [
		ParameterType.Group,
		ParameterType.Monitor,
		ParameterType.Space,
		ParameterType.System
	];

	return _.flatMap(_.zip<any>(view, data.groups), g =>
		_(_.zip<any>(g[0].items, g[1].params))
			.reject(p => _.includes(skip, p[0].type))
			.map(p => ({
				key: p[0].key,
				value: p[1]
			}))
			.value()
	);
}

function getMonitorParams(monitor: Monitor, metadata: Parameter): Parameter[] {
	if (!metadata) {
		return monitor.map.map(item => ({
			key: item.key,
			name: item.key,
			value: item.value
		}));
	}

	function recurse(params: Parameter[]) {
		return params.map(param => {
			const kv = _.find(monitor.map, { key: param.key });
			return Object.assign({}, param, {
				items: recurse(param.items || []),
				value: kv && kv.value
			});
		});
	}
	return recurse(metadata.items || []);
}

export function mapMonitorToView(monitor: Monitor, metadata: Parameter) {
	const params = getMonitorParams(monitor, metadata);
	return {
		name: monitor.name,
		monitorClass: monitor.monitorClass,
		params: _(params)
			.reject({ type: ParameterType.Group })
			.map(param => param.value)
			.value(),
		groups: _(params)
			.filter({ type: ParameterType.Group })
			.map(group => ({
				params: group.items.map(param => param.value)
			}))
			.value()
	};
}

export function mapAgentsToView(pit: Pit): {} {
	if (!pit)
		return { groups: [] };

	const { agents } = pit;
	return {
		agents: agents.map(agent => ({
			name: agent.name,
			location: agent.agentUrl,
			monitors: agent.monitors.map(monitor => {
				const metadata = findMonitorMetadata(pit, monitor.monitorClass);
				return mapMonitorToView(monitor, metadata);
			})
		}))
	};
}

function mapAgentsFromView(pit: Pit, data: AgentsFormData): Agent[] {
	return data.agents.map(agent => ({
		name: agent.name,
		agentUrl: agent.location,
		monitors: agent.monitors.map(monitor => {
			const monitorSchema = findMonitorMetadata(pit, monitor.monitorClass);
			return {
				name: monitor.name,
				monitorClass: monitor.monitorClass,
				map: _.concat<Parameter>(
					monitor.params.map((value, index) => {
						const param = monitorSchema.items[index];
						return {
							key: param.key,
							value: value || undefined
						};
					}),
					_.flatMap(monitor.groups, (group, i) => {
						const groupSchema = monitorSchema.items[i];
						return group.params.map((value, j) => {
							const param = groupSchema.items[j];
							return {
								key: param.key,
								value: value || undefined
							};
						});
					})
				).filter(param => !_.isUndefined(param.value))
			};
		})
	}));
}

export function findMonitorMetadata(pit: Pit, key: string): Parameter {
	for (const monitor of pit.metadata.monitors) {
		const ret = findByTypeKey(monitor, ParameterType.Monitor, key);
		if (ret) {
			return ret;
		}
	}
	return null;
}

function findByTypeKey(param: Parameter, type: string, key: string): Parameter {
	if (param.type === type) {
		if (param.key === key) {
			return param;
		}
	}

	for (const item of param.items || []) {
		const ret = findByTypeKey(item, type, key);
		if (ret) {
			return ret;
		}
	}

	return null;
}

export function validateParameter(param: Parameter, value: string): string {
	if (param.type === ParameterType.Space)
		return null;

	const isRequired = _.isUndefined(param.optional) || !param.optional;
	if (isRequired && _.isEmpty(value)) {
		return validationMessages.required;
	}

	return null;
}

function checkDefine(param: Parameter): boolean {
	const skip = [
		ParameterType.Group,
		ParameterType.Monitor,
		ParameterType.Space
	];
	return _.every(param.items || [], checkDefine) && (
		_.includes(skip, param.type) || param.optional || !_.isEmpty(param.value)
	);
}
