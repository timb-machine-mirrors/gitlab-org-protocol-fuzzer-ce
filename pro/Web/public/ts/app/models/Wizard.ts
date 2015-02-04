/// <reference path="../reference.ts" />

module Peach {
	"use strict";

	export module QuestionTypes {
		export var String = "string";
		export var Hex = "hex";
		export var Range = "range";
		export var Ipv4 = "ipv4";
		export var Ipv6 = "ipv6";
		export var HwAddress = "hwaddr";
		export var Iface = "iface";
		export var Choice = "choice";
		export var Jump = "jump";
		export var Intro = "intro";
		export var Done = "done";
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
		title: string;
		next?: ITrackNext;
		nextPrompt?: string;
		backPrompt?: string;
		template?: IWizardTemplate;

		// dynamic values
		isComplete?: boolean;
		agents?: Agent[];
		history: number[];

		Begin(): ng.IPromise<any>;
		Finish();
		Restart();

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
