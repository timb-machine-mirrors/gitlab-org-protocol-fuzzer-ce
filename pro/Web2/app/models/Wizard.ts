import { MakeLowerEnum } from '../utils';
import { Agent, Monitor } from './Pit';

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

export interface Choice {
	a?: string;
	value?: any;
	next?: number;
}

export interface Question {
	id: number;
	type: string;

	q?: string;
	key?: string;
	choice?: Choice[];
	shortName?: string;
	next?: number;
	required?: boolean;
	value?: any;
	defaults?: any[];
	rangeMin?: number;
	rangeMax?: number;
}

export interface WizardTemplate {
	qa: Question[];
	monitors: Monitor[];
}

export interface Track {
	// readonly
	name: string;
	start: string;
	steps: string;
	finish: string;
	next?: TrackNext;
	nextPrompt?: string;
	backPrompt?: string;
	template?: WizardTemplate;

	// dynamic values
	isComplete?: boolean;
	agents?: Agent[];
	history: number[];

	Begin(): Promise<any>;
	Finish(): void;

	GetQuestionById(id: number): Question;
	GetQuestionByKey(key: string): Question;
	GetValueByKey(key: string): any;

	IsValid(): boolean;
}

export interface TrackNext {
	state: string;
	params?: any;
}
