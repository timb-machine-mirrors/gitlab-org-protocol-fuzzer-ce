/// <reference path="../reference.ts" />

namespace Peach {
	export namespace QuestionTypes {
		export const String = "string";
		export const Hex = "hex";
		export const Range = "range";
		export const Ipv4 = "ipv4";
		export const Ipv6 = "ipv6";
		export const HwAddress = "hwaddr";
		export const Iface = "iface";
		export const Choice = "choice";
		export const Jump = "jump";
		export const Intro = "intro";
		export const Done = "done";
	}

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
