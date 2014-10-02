module DashApp {
	export class StartJobController {
		private modalInstance: ng.ui.bootstrap.IModalServiceInstance;


		static $inject = ["$scope","$modalInstance"]; 

		constructor($scope: ViewModelScope, $modalInstance: ng.ui.bootstrap.IModalServiceInstance) {
			this.modalInstance = $modalInstance;
			$scope.vm = this;
		}

		private _job: Models.Job;

		public get Job(): Models.Job {
			return this._job;
		}

		public set Job(job: Models.Job) {
			if (this._job !== job) {
				this._job = job;
			}
		}

		public seedChecked: boolean;
		public iterationStartChecked: boolean;
		public iterationEndChecked: boolean;

		public startJob() {
			console.info(JSON.stringify(this._job));
			this.modalInstance.close(this._job);
		}

		public cancel() {
			this.modalInstance.dismiss();
		}
	}
}