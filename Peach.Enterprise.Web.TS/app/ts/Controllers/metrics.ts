/// <reference path="../services/peach.ts" />

module DashApp {
	"use strict";

	export class MetricsController {

		static $inject = ["$scope", "poller", "peachService"];

		constructor($scope, poller, peachService: Services.IPeachService) {

			//$scope.pit = peachService.GetPit().query();
			//peachService.GetPit(1, (data: Models.Pit) => {
			//	$scope.pit = data;
			//});

			$scope.metrics_faultsOverTime_data = [
				{ x: new Date(2014, 4, 5, 1, 1, 0, 1), y: 1 },
				{ x: new Date(2014, 4, 5, 2, 1, 0, 1), y: 2 },
				{ x: new Date(2014, 4, 5, 3, 1, 0, 1), y: 1 },
				{ x: new Date(2014, 4, 5, 4, 1, 0, 1), y: 3 },
				{ x: new Date(2014, 4, 5, 5, 1, 0, 1), y: 1 },
				{ x: new Date(2014, 4, 5, 6, 1, 0, 1), y: 1 }];

			$scope.metrics_faultsOverTime_options = {
				axes: {
					x: {
						type: "date",
						tooltipFormatter: function (d) { return moment(d).fromNow(); },
						key: "x"
					},
					y: { type: "linear" },
				},
				series: [
					{
						y: "y",
						label: "faults over time",
						color: "steelblue",
						thickness: "2px",
						type: "line"
					}
				],
				lineMode: "linear",
				tension: 0.7,
				tooltipMode: "default"
			};

			$scope.dataMetricsMutator = [
				{
					"mutator": "StringMutator",
					"elementCount": 10,
					"iterationCount": 5000,
					"bucketCount": 3,
					"faultCount": 200
				},
				{
					"mutator": "StringMutator",
					"elementCount": 10,
					"iterationCount": 5000,
					"bucketCount": 3,
					"faultCount": 200
				},
				{
					"mutator": "StringMutator",
					"elementCount": 10,
					"iterationCount": 5000,
					"bucketCount": 3,
					"faultCount": 200
				},
				{
					"mutator": "StringMutator",
					"elementCount": 10,
					"iterationCount": 5000,
					"bucketCount": 3,
					"faultCount": 200
				}
			];
			$scope.gridMetricsMutator = {
				data: "dataMetricsMutator",
				sortInfo: { fields: ["mutator"], directions: ["asc"] },
				columnDefs: [
					{ field: "mutator", displayName: "Mutator" },
					{ field: "elementCount", displayName: "Element Count" },
					{ field: "iterationCount", displayName: "Iteration Count" },
					{ field: "bucketCount", displayName: "Bucket Count" },
					{ field: "faultCount", displayName: "Fault Count" }
				]
			};

		$scope.dataMetricsElement = [
				{
					"element": "FooData",
					"mutatorCount": 3,
					"iterationCount": 5000,
					"bucketCount": 3,
					"faultCount": 200
				},
				{
					"element": "FooData",
					"mutatorCount": 3,
					"iterationCount": 5000,
					"bucketCount": 3,
					"faultCount": 200
				},
				{
					"element": "FooData",
					"mutatorCount": 3,
					"iterationCount": 5000,
					"bucketCount": 3,
					"faultCount": 200
				},
				{
					"element": "FooData",
					"mutatorCount": 3,
					"iterationCount": 5000,
					"bucketCount": 3,
					"faultCount": 200
				}
			];
			$scope.gridMetricsElement = {
				data: "dataMetricsElement",
				sortInfo: { fields: ["element"], directions: ["asc"] },
				columnDefs: [
					{ field: "element", displayName: "Element" },
					{ field: "mutatorCount", displayName: "Mutator Count" },
					{ field: "iterationCount", displayName: "Iteration Count" },
					{ field: "bucketCount", displayName: "Bucket Count" },
					{ field: "faultCount", displayName: "Fault Count" }
				]
			};

		$scope.dataMetricsState = [
				{
					"element": "FooData",
					"mutatorCount": 3,
					"iterationCount": 5000,
					"bucketCount": 3,
					"faultCount": 200
				}
			];
			$scope.gridMetricsState = {
				data: "dataMetricsState",
				sortInfo: { fields: ["element"], directions: ["asc"] },
				columnDefs: [
					{ field: "element", displayName: "Element" },
					{ field: "mutatorCount", displayName: "Mutator Count" },
					{ field: "iterationCount", displayName: "Iteration Count" },
					{ field: "bucketCount", displayName: "Bucket Count" },
					{ field: "faultCount", displayName: "Fault Count" }
				]
			};

		$scope.dataMetricsData = [
				{
					"element": "FooData",
					"mutatorCount": 3,
					"iterationCount": 5000,
					"bucketCount": 3,
					"faultCount": 200
				}
			];
			$scope.gridMetricsData = {
				data: "dataMetricsData",
				sortInfo: { fields: ["element"], directions: ["asc"] },
				columnDefs: [
					{ field: "element", displayName: "Element" },
					{ field: "mutatorCount", displayName: "Mutator Count" },
					{ field: "iterationCount", displayName: "Iteration Count" },
					{ field: "bucketCount", displayName: "Bucket Count" },
					{ field: "faultCount", displayName: "Fault Count" }
				]
			};


		$scope.dataMetricsBucket = [
				{
					"element": "FooData",
					"mutatorCount": 3,
					"iterationCount": 5000,
					"bucketCount": 3,
					"faultCount": 200
				}
			];
			$scope.gridMetricsBucket = {
				data: "dataMetricsBucket",
				sortInfo: { fields: ["element"], directions: ["asc"] },
				columnDefs: [
					{ field: "element", displayName: "Element" },
					{ field: "mutatorCount", displayName: "Mutator Count" },
					{ field: "iterationCount", displayName: "Iteration Count" },
					{ field: "bucketCount", displayName: "Bucket Count" },
					{ field: "faultCount", displayName: "Fault Count" }
				]
			};


		}
	}
}