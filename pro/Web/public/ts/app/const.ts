module Peach.Constants {
	"use strict";

	export module Vendor {
		export var VisDataSet = 'visDataSet';
	}

	export module Directives {
		export var Agent = 'peachAgent';
		export var Combobox = 'peachCombobox';
		export var Monitor = 'peachMonitor';
		export var Parameter = 'peachParameter';
		export var ParameterInput = 'peachParameterInput';
		export var Question = 'peachQuestion';
		export var Test = 'peachTest';
		export var Unique = 'peachUnique';
		export var UniqueChannel = 'peachUniqueChannel';
		export var Unsaved = 'peachUnsaved';
		export var Range = 'peachRange';
		export var Integer = 'integer';
		export var HexString = 'hexstring';
	}

	export module Controllers {
		export var Combobox = 'ComboboxController';
		export var UniqueChannel = 'UniqueChannelController';
		export var Unsaved = 'UnsavedController';
		export var Agent = 'AgentController';
		export var Monitor = 'MonitorController';
		export var Parameter = 'ParameterController';
		export var Test = 'TestController';
		export var Combobox = 'ComboboxController';
	}

	export module Services {
		export var Pit = 'PitService';
		export var Unique = 'UniqueService';
		export var HttpError = 'HttpErrorService';
		export var Job = 'JobService';
		export var Test = 'TestService';
		export var Wizard = 'WizardService';
	}

	export module Urls {
		export var Libraries = '/p/libraries';
		export var Pits = '/p/pits';
		export var Jobs = '/p/jobs';
		export var PeachMonitors = '/p/conf/wizard/monitors';
		export var TestStart = '/p/conf/wizard/test/start';
	}

	export module Tracks {
		export var Default = 'default';
		export var Intro = 'intro';
		export var Vars = 'vars';
		export var Fault = 'fault';
		export var Data = 'data';
		export var Auto = 'auto';
		export var Test = 'test';
		export var Done = 'done';
	}

	export module Metrics {
		export var BucketTimeline = 'bucketTimeline';
		export var FaultTimeline = 'faultTimeline';
		export var Mutators = 'mutators';
		export var Elements = 'elements';
		export var Dataset = 'dataset';
		export var States = 'states';
		export var Buckets = 'buckets';
	}

	export module Templates {
		export var Dashboard = 'html/dashboard.html';
		export var Faults = 'html/faults.html';
		export var Metrics = 'html/metrics.html';
		export function Metric(metric: string): string {
			return 'html/metrics/' + metric + '.html';
		}
		export var BucketTimelineItem = 'bucketTimelineItem.html';
		export module Modal {
			export var CopyPit = 'html/modal/CopyPit.html';
			export var PitLibrary = 'html/modal/PitLibrary.html';
			export var StartJob = 'html/modal/StartJob.html';
			export var NewVar = 'html/modal/NewVar.html';
			export var Unsaved = 'html/modal/Unsaved.html';
		}
		export module Wizard {
			export var Step = "html/wizard.html";
			export var Intro = 'html/wizard/intro.html';
			export var Test = 'html/wizard/test.html';
			export var Done = 'html/wizard/done.html';
			export function TrackIntro(track: string): string {
				return 'html/wizard/' + track + '/intro.html';
			}
			export function TrackDone(track: string): string {
				return 'html/wizard/' + track + '/intro.html';
			}
		}
		export module Config {
			export var Variables = 'html/cfg/variables.html';
			export var Monitoring = 'html/cfg/monitoring.html';
			export var Test = 'html/cfg/test.html';
		}
		export module Directives {
			export var Agent = 'html/directives/agent.html';
			export var Monitor = 'html/directives/monitor.html';
			export var Parameter = 'html/directives/parameter.html';
			export var ParameterInput = 'html/directives/parameter-input.html';
			export var Question = 'html/directives/question.html';
			export var Combobox = 'html/directives/combobox.html';
			export var Test = 'html/directives/test.html';
		}
	}

	export module Routes {
		export var Home = '/';
		export var Faults = '/faults/:bucket';
		export var Metrics = '/metrics/:metric';
		export var WizardPrefix = '/quickstart/';
		export var WizardStep = WizardPrefix + ':step';
		export var ConfigMonitoring = '/cfg/monitors';
		export var ConfigVariables = '/cfg/variables';
		export var ConfigTest = '/cfg/test';
	}
}
