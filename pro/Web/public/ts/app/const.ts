module Peach.Constants {
	 "use strict";

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
}
