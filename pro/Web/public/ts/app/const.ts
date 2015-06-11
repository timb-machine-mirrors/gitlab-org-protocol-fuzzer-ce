﻿/// <reference path="reference.ts" />

module Peach.C {
	"use strict";

	export var ViewModel = 'vm';

	export module Vendor {
		export var VisDataSet = 'VisDataSet';
	}

	export module Events {
		export var PitChanged = 'PitChanged';
	}

	export module Directives {
		export var Agent = 'peachAgent';
		export var AutoFocus = 'peachAutoFocus';
		export var Combobox = 'peachCombobox';
		export var Faults = 'peachFaults';
		export var Jobs = 'peachJobs';
		export var Monitor = 'peachMonitor';
		export var Parameter = 'peachParameter';
		export var ParameterInput = 'peachParameterInput';
		export var Test = 'peachTest';
		export var Unique = 'peachUnique';
		export var UniqueChannel = 'peachUniqueChannel';
		export var Unsaved = 'peachUnsaved';
		export var Range = 'peachRange';
		export var Integer = 'peachInteger';
		export var HexString = 'peachHexstring';
		export var Ratio = 'stRatio';
	}

	export module Validation {
		export var HexString = 'hexstring';
		export var Integer = 'integer';
		export var RangeMax = 'rangeMax';
		export var RangeMin = 'rangeMin';
	}

	export module Controllers {
		export var Agent = 'AgentController';
		export var Combobox = 'ComboboxController';
		export var Faults = 'FaultsDirectiveController';
		export var Jobs = 'JobsDirectiveController';
		export var Monitor = 'MonitorController';
		export var Parameter = 'ParameterController';
		export var Test = 'TestController';
		export var UniqueChannel = 'UniqueChannelController';
		export var Unsaved = 'UnsavedController';
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
		export var PitUrl = '/p/pits/:id';
		export var Jobs = '/p/jobs';
		export var JobUrl = '/p/jobs/:id';
		export var PeachMonitors = '/p/pits/monitors';
	}

	export module Tracks {
		export var Intro = 'intro';
		export var Vars = 'vars';
		export var Fault = 'fault';
		export var Data = 'data';
		export var Auto = 'auto';
		export var Test = 'test';
	}

	export interface IMetric {
		id: string;
		name: string;
	}

	export module Metrics {
		export var BucketTimeline: IMetric = {
			id: 'bucketTimeline', name: 'Bucket Timeline'
		};
		export var FaultTimeline: IMetric = {
			id: 'faultTimeline', name: 'Faults Over Time'
		};
		export var Mutators: IMetric = {
			id: 'mutators', name: 'Mutators'
		};
		export var Elements: IMetric = {
			id: 'elements', name: 'Elements'
		};
		export var States: IMetric = {
			id: 'states', name: 'States'
		};
		export var Dataset: IMetric = {
			id: 'dataset', name: 'Datasets'
		};
		export var Buckets: IMetric = {
			id: 'buckets', name: 'Buckets'
		};
	}

	export var MetricsList: IMetric[] = [
		Metrics.BucketTimeline,
		Metrics.FaultTimeline,
		Metrics.Mutators,
		Metrics.Elements,
		Metrics.States,
		Metrics.Dataset,
		Metrics.Buckets
	];

	export module Templates {
		export var UiView = '<div ui-view></div>';
		export var Home = 'html/home.html';
		export var Jobs = 'html/jobs.html';
		export var Library = 'html/library.html';
		export var Error = 'html/error.html';
		export module Job {
			export var Dashboard = 'html/job/dashboard.html';
			export module Faults {
				export var Summary = 'html/job/faults/summary.html';
				export var Detail = 'html/job/faults/detail.html';
			}
			export var MetricPage = 'html/job/metrics/:metric.html';
			export var BucketTimelineItem = 'bucketTimelineItem.html';
		}
		export module Pit {
			export var Configure = 'html/pit/configure.html';
			export module Wizard {
				export var Intro = 'html/pit/wizard/intro.html';
				export var Track = 'html/pit/wizard/track.html';
				export var TrackIntro = 'html/pit/wizard/:track/intro.html';
				export var Question = 'html/pit/wizard/question.html';
				export var TrackDone = 'html/pit/wizard/:track/done.html';
				export var Test = 'html/pit/wizard/test.html';
				export var QuestionType = 'html/pit/q/:type.html';
			}
			export module Advanced {
				export var Variables = 'html/pit/advanced/variables.html';
				export var Monitoring = 'html/pit/advanced/monitoring.html';
				export var Test = 'html/pit/advanced/test.html';
			}
		}
		export module Modal {
			export var Confirm = 'html/modal/Confirm.html';
			export var NewConfig = 'html/modal/NewConfig.html';
			export var NewVar = 'html/modal/NewVar.html';
			export var PitLibrary = 'html/modal/PitLibrary.html';
			export var StartJob = 'html/modal/StartJob.html';
		}
		export module Directives {
			export var Agent = 'html/directives/agent.html';
			export var Combobox = 'html/directives/combobox.html';
			export var Faults = 'html/directives/faults.html';
			export var Jobs = 'html/directives/jobs.html';
			export var Monitor = 'html/directives/monitor.html';
			export var Parameter = 'html/directives/parameter.html';
			export var ParameterInput = 'html/directives/parameter-input.html';
			export var Question = 'html/directives/question.html';
			export var Test = 'html/directives/test.html';
		}
	}

	export module States {
		export var Main = 'main';
		export var MainHome = 'main.home';
		export var MainJobs = 'main.jobs';
		export var MainLibrary = 'main.library';
		export var MainError = 'main.error';

		export var Job = 'job';
		export var JobFaults = 'job.faults';
		export var JobFaultsDetail = 'job.faults.detail';
		export var JobMetrics = 'job.metrics';

		export var Pit = 'pit';

		export var PitWizard = 'pit.wizard';
		export var PitWizardTest = 'pit.wizard.test';

		export var PitAdvanced = 'pit.advanced';
		export var PitAdvancedVariables = 'pit.advanced.variables';
		export var PitAdvancedMonitoring = 'pit.advanced.monitoring';
		export var PitAdvancedTest = 'pit.advanced.test';
	}
}
