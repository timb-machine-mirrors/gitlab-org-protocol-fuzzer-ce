/// <reference path="../reference.ts" />

namespace Peach {
	export var QuestionTypes = {
		String : '',
		Hex    : '',
		Range  : '',
		Ipv4   : '',
		Ipv6   : '',
		Hwaddr : '',
		Iface  : '',
		Enum   : '',
		Bool   : '',
		User   : '',
		Choice : '',
		Jump   : '',
		Intro  : '',
		Done   : '',
		Combo  : ''
	};
	MakeLowerEnum(QuestionTypes);

	export interface IChoice {
		a?: string;
		value?: any;
		next?: number;
	}

	export interface IQuestion {
		id: number;
		type: string;

		q?: string;
		key?: string;
		choice?: IChoice[];
		shortName?: string;
		next?: number;
		required?: boolean;
		value?: any;
		defaults?: any[];
		rangeMin?: number;
		rangeMax?: number;
	}

	export interface IWizardTemplate {
		qa: IQuestion[];
		monitors: IMonitor[];
	}

	export interface ITrack {
		// readonly
		name: string;
		start: string;
		steps: string;
		finish: string;
		next?: ITrackNext;
		nextPrompt?: string;
		backPrompt?: string;
		template?: IWizardTemplate;

		// dynamic values
		isComplete?: boolean;
		agents?: IAgent[];
		history: number[];

		Begin(): ng.IPromise<any>;
		Finish(): void;

		GetQuestionById(id: number): IQuestion;
		GetQuestionByKey(key: string): IQuestion;
		GetValueByKey(key: string): any;

		IsValid(): boolean;
	}

	export interface ITrackNext {
		state: string;
		params?: any;
	}
}
