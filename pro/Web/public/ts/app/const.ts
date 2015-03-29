/// <reference path="reference.ts" />

module Peach.C {
	"use strict";

	export module Vendor {
		export var VisDataSet = 'visDataSet';
	}

	export module Directives {
		export var Agent = 'peachAgent';
		export var AutoFocus = 'peachAutoFocus';
		export var Combobox = 'peachCombobox';
		export var Monitor = 'peachMonitor';
		export var Parameter = 'peachParameter';
		export var ParameterInput = 'peachParameterInput';
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

	export module Api {
		export var Libraries = '/p/libraries';
		export var Pits = '/p/pits';
		export var Jobs = '/p/jobs';
		export var PeachMonitors = '/p/pits/monitors';
	}

	export module Tracks {
		export var Default = 'default';
		export var Intro = 'intro';
		export var Vars = 'vars';
		export var Fault = 'fault';
		export var Data = 'data';
		export var Auto = 'auto';
		export var Test = 'test';
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
		export var Faults = 'html/faults/summary.html';
		export var FaultsDetail = 'html/faults/detail.html';
		export var MetricPage = 'html/metrics/:metric.html';
		export var BucketTimelineItem = 'bucketTimelineItem.html';
		export module Modal {
			export var CopyPit = 'html/modal/CopyPit.html';
			export var PitLibrary = 'html/modal/PitLibrary.html';
			export var StartJob = 'html/modal/StartJob.html';
			export var NewVar = 'html/modal/NewVar.html';
			export var Unsaved = 'html/modal/Unsaved.html';
		}
		export module Wizard {
			export var Intro = 'html/wizard/intro.html';
			export var Track = 'html/wizard/track.html';
			export var TrackIntro = 'html/wizard/:track/intro.html';
			export var Question = 'html/wizard/question.html';
			export var TrackDone = 'html/wizard/:track/done.html';
			export var Test = 'html/wizard/test.html';
			export var QuestionType = 'html/q/:type.html';
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

	export module States {
		export var Home = 'home';
		export var Faults = 'faults';
		export var FaultsDetail = 'faults.detail';
		export var Metrics = 'metrics';

		export var Wizard = 'wizard';
		export var WizardIntro = 'wizard.intro';
		export var WizardQuestion = 'wizard.question';
		export var WizardReview = 'wizard.review';

		export var Config = 'config';
		export var ConfigVariables = 'config.variables';
		export var ConfigMonitoring = 'config.monitoring';
		export var ConfigTest = 'config.test';
	}
}
