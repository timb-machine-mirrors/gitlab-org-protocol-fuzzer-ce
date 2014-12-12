/// <reference path="../reference.ts" />

module Peach.Models {
	"use strict";

	export class QuestionTypes {
		static String = "string";
		static HexString = "hex";
		static Number = "int";
		static Range = "range";
		static IPV4 = "ipv4";
		static IPV6 = "ipv6";
		static MACAddress = "hwaddr";
		static NetworkInterface = "iface";
		static OnCall = "oncall";
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
