/// <reference path="../reference.ts" />

module Peach {
	"use strict";

	export class QuestionTypes {
		static String = "string";
		static Hex = "hex";
		static Range = "range";
		static Ipv4 = "ipv4";
		static Ipv6 = "ipv6";
		static HwAddress = "hwaddr";
		static Iface = "iface";
		static Choice = "choice";
		static Jump = "jump";
		static Intro = "intro";
		static Done = "done";
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
		qref?: string;
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
		qref?: string;
		next?: string;
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
	}
}
