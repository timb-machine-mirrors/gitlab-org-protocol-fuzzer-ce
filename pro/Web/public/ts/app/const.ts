﻿/// <reference path="reference.ts" />

namespace Peach.C {
	export const ViewModel = 'vm';

	export namespace Vendor {
		export const VisDataSet = 'VisDataSet';
	}

	export namespace Events {
		export const PitChanged = 'PitChanged';
	}

	export namespace Directives {
		export const Agent = 'peachAgent';
		export const AutoFocus = 'peachAutoFocus';
		export const Combobox = 'peachCombobox';
		export const Defines = 'peachDefines';
		export const Faults = 'peachFaults';
		export const Jobs = 'peachJobs';
		export const Monitor = 'peachMonitor';
		export const Parameter = 'peachParameter';
		export const ParameterInput = 'peachParameterInput';
		export const Test = 'peachTest';
		export const Unique = 'peachUnique';
		export const UniqueChannel = 'peachUniqueChannel';
		export const Unsaved = 'peachUnsaved';
		export const Range = 'peachRange';
		export const Integer = 'peachInteger';
		export const HexString = 'peachHexstring';
		export const Ratio = 'stRatio';
	}

	export namespace Validation {
		export const HexString = 'hexstring';
		export const Integer = 'integer';
		export const RangeMax = 'rangeMax';
		export const RangeMin = 'rangeMin';
	}

	export namespace Controllers {
		export const Agent = 'AgentController';
		export const Combobox = 'ComboboxController';
		export const Defines = 'DefinesController';
		export const Faults = 'FaultsDirectiveController';
		export const Jobs = 'JobsDirectiveController';
		export const Monitor = 'MonitorController';
		export const Parameter = 'ParameterController';
		export const ParameterInput = 'ParameterInputController';
		export const Test = 'TestController';
		export const UniqueChannel = 'UniqueChannelController';
		export const Unsaved = 'UnsavedController';
	}

	export namespace Services {
		export const Pit = 'PitService';
		export const Unique = 'UniqueService';
		export const HttpError = 'HttpErrorService';
		export const Job = 'JobService';
		export const Test = 'TestService';
		export const Wizard = 'WizardService';
	}

	export namespace Api {
		export const Libraries = '/p/libraries';
		export const Pits = '/p/pits';
		export const PitUrl = '/p/pits/:id';
		export const Jobs = '/p/jobs';
		export const JobUrl = '/p/jobs/:id';
	}

	export namespace Tracks {
		export const Intro = 'intro';
		export const Vars = 'Vars';
		export const Fault = 'fault';
		export const Data = 'data';
		export const Auto = 'auto';
		export const Test = 'test';
	}

	export interface IMetric {
		id: string;
		name: string;
	}

	export namespace Metrics {
		export const BucketTimeline: IMetric = {
			id: 'bucketTimeline', name: 'Bucket Timeline'
		};
		export const FaultTimeline: IMetric = {
			id: 'faultTimeline', name: 'Faults Over Time'
		};
		export const Mutators: IMetric = {
			id: 'mutators', name: 'Mutators'
		};
		export const Elements: IMetric = {
			id: 'elements', name: 'Elements'
		};
		export const States: IMetric = {
			id: 'states', name: 'States'
		};
		export const Dataset: IMetric = {
			id: 'dataset', name: 'Datasets'
		};
		export const Buckets: IMetric = {
			id: 'buckets', name: 'Buckets'
		};
	}

	export const MetricsList: IMetric[] = [
		Metrics.BucketTimeline,
		Metrics.FaultTimeline,
		Metrics.Mutators,
		Metrics.Elements,
		Metrics.States,
		Metrics.Dataset,
		Metrics.Buckets
	];

	export namespace Templates {
		export const UiView = '<div ui-view></div>';
		export const Home = 'html/home.html';
		export const Jobs = 'html/jobs.html';
		export const Library = 'html/library.html';
		export const Error = 'html/error.html';
		export namespace Job {
			export const Dashboard = 'html/job/dashboard.html';
			export namespace Faults {
				export const Summary = 'html/job/faults/summary.html';
				export const Detail = 'html/job/faults/detail.html';
			}
			export const MetricPage = 'html/job/metrics/:metric.html';
			export const BucketTimelineItem = 'bucketTimelineItem.html';
		}
		export namespace Pit {
			export const Configure = 'html/pit/configure.html';
			export namespace Wizard {
				export const Intro = 'html/pit/wizard/intro.html';
				export const Track = 'html/pit/wizard/track.html';
				export const TrackIntro = 'html/pit/wizard/:track/intro.html';
				export const Question = 'html/pit/wizard/question.html';
				export const TrackDone = 'html/pit/wizard/:track/done.html';
				export const Test = 'html/pit/wizard/test.html';
				export const QuestionType = 'html/pit/q/:type.html';
			}
			export namespace Advanced {
				export const Variables = 'html/pit/advanced/variables.html';
				export const Monitoring = 'html/pit/advanced/monitoring.html';
				export const Test = 'html/pit/advanced/test.html';
			}
		}
		export namespace Modal {
			export const Confirm = 'html/modal/Confirm.html';
			export const Alert = 'html/modal/Alert.html';
			export const NewConfig = 'html/modal/NewConfig.html';
			export const NewVar = 'html/modal/NewVar.html';
			export const PitLibrary = 'html/modal/PitLibrary.html';
			export const StartJob = 'html/modal/StartJob.html';
			export const AddMonitor = 'html/modal/AddMonitor.html';
		}
		export namespace Directives {
			export const Agent = 'html/directives/agent.html';
			export const Combobox = 'html/directives/combobox.html';
			export const Defines = 'html/directives/defines.html';
			export const Faults = 'html/directives/faults.html';
			export const Jobs = 'html/directives/jobs.html';
			export const Monitor = 'html/directives/monitor.html';
			export const Parameter = 'html/directives/parameter.html';
			export const ParameterInput = 'html/directives/parameter-input.html';
			export const Question = 'html/directives/question.html';
			export const Test = 'html/directives/test.html';
		}
	}

	export namespace States {
		export const Main = 'main';
		export const MainHome = 'main.home';
		export const MainJobs = 'main.jobs';
		export const MainLibrary = 'main.library';
		export const MainError = 'main.error';

		export const Job = 'job';
		export const JobFaults = 'job.faults';
		export const JobFaultsDetail = 'job.faults.detail';
		export const JobMetrics = 'job.metrics';

		export const Pit = 'pit';

		export const PitWizard = 'pit.wizard';
		export const PitWizardTest = 'pit.wizard.test';

		export const PitAdvanced = 'pit.advanced';
		export const PitAdvancedVariables = 'pit.advanced.variables';
		export const PitAdvancedMonitoring = 'pit.advanced.monitoring';
		export const PitAdvancedTest = 'pit.advanced.test';
	}
}
