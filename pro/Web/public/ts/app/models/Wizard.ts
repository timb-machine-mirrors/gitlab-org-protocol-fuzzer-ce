/// <reference path="../reference.ts" />

module Peach.Models {
	"use strict";

	export interface IChoice {
		value: any;
		a: string;
		next: number;
	}

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

	export class Question {
		id: number;

		q: string;
		qref: string;

		type: string;

		choice: IChoice[];

		shortName: string;

		key: string;
		next: number;

		required: boolean = true;
		value: any;

		defaults: any[] = [];

		rangeMin: number;
		rangeMax: number;

		constructor(question?: Question) {
			if (question) {
				this.id = question.id;
				this.q = question.q;
				this.qref = question.qref;
				this.type = question.type;
				this.choice = question.choice;
				this.shortName = question.shortName;
				this.key = question.key;
				this.value = question.value;
				this.defaults = question.defaults;
				this.rangeMin = question.rangeMin;
				this.rangeMax = question.rangeMax;
				this.next = question.next;
				if (question.required !== undefined) {
					this.required = question.required;
				}
			}
		}

		public static CreateQA(questions: Question[]): Question[] {
			return questions.map((q: Question) => { return new Question(q); });
		}
	}
}
