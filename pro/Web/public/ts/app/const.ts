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
		export var Home = 'html/home.html';
		export var Jobs = 'html/jobs.html';
		export var Library = 'html/library.html';
		export var Templates = 'html/templates.html';
		export var Job = 'html/dashboard.html';
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
		export var Main = 'main';
		export var MainHome = 'main.home';
		export var MainJobs = 'main.jobs';
		export var MainLibrary = 'main.library';
		export var MainTemplates = 'main.templates';

		export var Job = 'job';
		export var JobDashboard = 'job.dashboard';
		export var JobFaults = 'job.faults';
		export var JobFaultsDetail = 'job.faults.detail';
		export var JobMetrics = 'job.metrics';

		export var Pit = 'pit';
		export var PitWizard = 'pit.wizard';
		export var PitWizardIntro = 'pit.wizard.intro';
		export var PitWizardQuestion = 'pit.wizard.question';
		export var PitWizardReview = 'pit.wizard.review';
		export var PitConfig = 'pit.config';
		export var PitConfigVariables = 'pit.config.variables';
		export var PitConfigMonitoring = 'pit.config.monitoring';
		export var PitConfigTest = 'pit.config.test';
	}
}
