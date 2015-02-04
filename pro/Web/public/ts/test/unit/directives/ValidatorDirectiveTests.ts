/// <reference path="../reference.ts" />

'use strict';

interface ITestValidatorsScope extends ng.IScope {
	form: ng.IFormController;
	model: any;
}

describe("Peach", () => {
	beforeEach(module('Peach'));

	describe('RangeDirective', () => {
		var $compile: ng.ICompileService;
		var scope: ITestValidatorsScope;
		var element: ng.IAugmentedJQuery;
		var modelCtrl: ng.INgModelController;

		beforeEach(inject(($injector: ng.auto.IInjectorService) => {
			$compile = $injector.get('$compile');
			var $rootScope = $injector.get('$rootScope');
			scope = <ITestValidatorsScope> $rootScope.$new();
		}));

		describe('range-min: 1', () => {
			beforeEach(() => {
				var html = pithy.form({ name: 'form' }, [
					pithy.input({
						type: 'text',
						name: 'input',
						'ng-model': 'model',
						'peach:range': 'true',
						'peach:range-min': '1'
					})
				]).toString();

				element = $compile(html)(scope);
				modelCtrl = <ng.INgModelController> scope.form['input'];
			});

			it("ok: ''", () => {
				modelCtrl.$setViewValue('');
				scope.$digest();
				expect(scope.form.$valid).toBe(true);
				expect(_.has(modelCtrl.$error, 'rangeMin')).toBe(false);
				expect(_.has(modelCtrl.$error, 'rangeMax')).toBe(false);
			});

			it("ok: 1", () => {
				modelCtrl.$setViewValue('1');
				scope.$digest();
				expect(scope.form.$valid).toBe(true);
				expect(_.has(modelCtrl.$error, 'rangeMin')).toBe(false);
				expect(_.has(modelCtrl.$error, 'rangeMax')).toBe(false);
			});

			it("min: 0", () => {
				modelCtrl.$setViewValue('0');
				scope.$digest();
				expect(scope.form.$valid).toBe(false);
				expect(_.has(modelCtrl.$error, 'rangeMin')).toBe(true);
				expect(_.has(modelCtrl.$error, 'rangeMax')).toBe(false);
			});

			it("min: -1", () => {
				modelCtrl.$setViewValue('-1');
				scope.$digest();
				expect(scope.form.$valid).toBe(false);
				expect(_.has(modelCtrl.$error, 'rangeMin')).toBe(true);
				expect(_.has(modelCtrl.$error, 'rangeMax')).toBe(false);
			});

			it("min: x", () => {
				modelCtrl.$setViewValue('x');
				scope.$digest();
				expect(scope.form.$valid).toBe(false);
				expect(_.has(modelCtrl.$error, 'rangeMin')).toBe(true);
				expect(_.has(modelCtrl.$error, 'rangeMax')).toBe(false);
			});
		});

		describe('range-max: 10', () => {
			beforeEach(() => {
				var html = pithy.form({ name: 'form' }, [
					pithy.input({
						type: 'text',
						name: 'input',
						'ng-model': 'model',
						'peach:range': 'true',
						'peach:range-max': '10'
					})
				]).toString();

				element = $compile(html)(scope);
				modelCtrl = <ng.INgModelController> scope.form['input'];
			});

			it("ok: ''", () => {
				modelCtrl.$setViewValue('');
				scope.$digest();
				expect(scope.form.$valid).toBe(true);
				expect(_.has(modelCtrl.$error, 'rangeMin')).toBe(false);
				expect(_.has(modelCtrl.$error, 'rangeMax')).toBe(false);
			});

			it("ok: 10", () => {
				modelCtrl.$setViewValue('10');
				scope.$digest();
				expect(scope.form.$valid).toBe(true);
				expect(_.has(modelCtrl.$error, 'rangeMin')).toBe(false);
				expect(_.has(modelCtrl.$error, 'rangeMax')).toBe(false);
			});

			it("max: 11", () => {
				modelCtrl.$setViewValue('11');
				scope.$digest();
				expect(scope.form.$valid).toBe(false);
				expect(_.has(modelCtrl.$error, 'rangeMin')).toBe(false);
				expect(_.has(modelCtrl.$error, 'rangeMax')).toBe(true);
			});

			it("ok: -1", () => {
				modelCtrl.$setViewValue('-1');
				scope.$digest();
				expect(scope.form.$valid).toBe(true);
				expect(_.has(modelCtrl.$error, 'rangeMin')).toBe(false);
				expect(_.has(modelCtrl.$error, 'rangeMax')).toBe(false);
			});

			it("max: x", () => {
				modelCtrl.$setViewValue('x');
				scope.$digest();
				expect(scope.form.$valid).toBe(false);
				expect(_.has(modelCtrl.$error, 'rangeMin')).toBe(false);
				expect(_.has(modelCtrl.$error, 'rangeMax')).toBe(true);
			});
		});

		describe('range-min: 1, range-max: 10', () => {
			beforeEach(() => {
				var html = pithy.form({ name: 'form' }, [
					pithy.input({
						type: 'text',
						name: 'input',
						'ng-model': 'model',
						'peach:range': 'true',
						'peach:range-min': '1',
						'peach:range-max': '10'
					})
				]).toString();

				element = $compile(html)(scope);
				modelCtrl = <ng.INgModelController> scope.form['input'];
			});

			it("ok: ''", () => {
				modelCtrl.$setViewValue('');
				scope.$digest();
				expect(scope.form.$valid).toBe(true);
				expect(_.has(modelCtrl.$error, 'rangeMin')).toBe(false);
				expect(_.has(modelCtrl.$error, 'rangeMax')).toBe(false);
			});

			it("ok: 1", () => {
				modelCtrl.$setViewValue('1');
				scope.$digest();
				expect(scope.form.$valid).toBe(true);
				expect(_.has(modelCtrl.$error, 'rangeMin')).toBe(false);
				expect(_.has(modelCtrl.$error, 'rangeMax')).toBe(false);
			});

			it("ok: 10", () => {
				modelCtrl.$setViewValue('1');
				scope.$digest();
				expect(scope.form.$valid).toBe(true);
				expect(_.has(modelCtrl.$error, 'rangeMin')).toBe(false);
				expect(_.has(modelCtrl.$error, 'rangeMax')).toBe(false);
			});

			it("min: 0", () => {
				modelCtrl.$setViewValue('0');
				scope.$digest();
				expect(scope.form.$valid).toBe(false);
				expect(_.has(modelCtrl.$error, 'rangeMin')).toBe(true);
				expect(_.has(modelCtrl.$error, 'rangeMax')).toBe(false);
			});

			it("max: 11", () => {
				modelCtrl.$setViewValue('11');
				scope.$digest();
				expect(scope.form.$valid).toBe(false);
				expect(_.has(modelCtrl.$error, 'rangeMin')).toBe(false);
				expect(_.has(modelCtrl.$error, 'rangeMax')).toBe(true);
			});

			it("min, max: x", () => {
				modelCtrl.$setViewValue('x');
				scope.$digest();
				expect(scope.form.$valid).toBe(false);
				expect(_.has(modelCtrl.$error, 'rangeMin')).toBe(true);
				expect(_.has(modelCtrl.$error, 'rangeMax')).toBe(true);
			});
		});
	});

	describe('IntegerDirective', () => {
		var $compile: ng.ICompileService;
		var scope: ITestValidatorsScope;
		var element: ng.IAugmentedJQuery;
		var modelCtrl: ng.INgModelController;

		beforeEach(inject(($injector: ng.auto.IInjectorService) => {
			$compile = $injector.get('$compile');
			var $rootScope = $injector.get('$rootScope');
			scope = <ITestValidatorsScope> $rootScope.$new();

			var html = pithy.form({ name: 'form' }, [
				pithy.input({
					type: 'text',
					name: 'input',
					'ng-model': 'model',
					'integer': 'true'
				})
			]).toString();

			element = $compile(html)(scope);
			modelCtrl = <ng.INgModelController> scope.form['input'];
		}));

		it("ok: ''", () => {
			modelCtrl.$setViewValue('');
			scope.$digest();
			expect(scope.form.$valid).toBe(true);
			expect(_.has(modelCtrl.$error, 'integer')).toBe(false);
		});

		it("ok: 1", () => {
			modelCtrl.$setViewValue('1');
			scope.$digest();
			expect(scope.form.$valid).toBe(true);
			expect(_.has(modelCtrl.$error, 'integer')).toBe(false);
		});

		it("ok: +1", () => {
			modelCtrl.$setViewValue('+1');
			scope.$digest();
			expect(scope.form.$valid).toBe(true);
			expect(_.has(modelCtrl.$error, 'integer')).toBe(false);
		});

		it("ok: -1", () => {
			modelCtrl.$setViewValue('-1');
			scope.$digest();
			expect(scope.form.$valid).toBe(true);
			expect(_.has(modelCtrl.$error, 'integer')).toBe(false);
		});

		it("bad: 0x01", () => {
			modelCtrl.$setViewValue('0x01');
			scope.$digest();
			expect(scope.form.$valid).toBe(false);
			expect(_.has(modelCtrl.$error, 'integer')).toBe(true);
		});

		it("bad: 0.0", () => {
			modelCtrl.$setViewValue('0.0');
			scope.$digest();
			expect(scope.form.$valid).toBe(false);
			expect(_.has(modelCtrl.$error, 'integer')).toBe(true);
		});

		it("bad: x", () => {
			modelCtrl.$setViewValue('x');
			scope.$digest();
			expect(scope.form.$valid).toBe(false);
			expect(_.has(modelCtrl.$error, 'integer')).toBe(true);
		});
	});

	describe('HexDirective', () => {
		var $compile: ng.ICompileService;
		var scope: ITestValidatorsScope;
		var element: ng.IAugmentedJQuery;
		var modelCtrl: ng.INgModelController;

		beforeEach(inject(($injector: ng.auto.IInjectorService) => {
			$compile = $injector.get('$compile');
			var $rootScope = $injector.get('$rootScope');
			scope = <ITestValidatorsScope> $rootScope.$new();

			var html = pithy.form({ name: 'form' }, [
				pithy.input({
					type: 'text',
					name: 'input',
					'ng-model': 'model',
					'hexstring': 'true'
				})
			]).toString();

			element = $compile(html)(scope);
			modelCtrl = <ng.INgModelController> scope.form['input'];
		}));

		it("ok: ''", () => {
			modelCtrl.$setViewValue('');
			scope.$digest();
			expect(scope.form.$valid).toBe(true);
			expect(_.has(modelCtrl.$error, 'hexstring')).toBe(false);
		});

		it("ok: 1", () => {
			modelCtrl.$setViewValue('1');
			scope.$digest();
			expect(scope.form.$valid).toBe(true);
			expect(_.has(modelCtrl.$error, 'hexstring')).toBe(false);
		});

		it("ok: A", () => {
			modelCtrl.$setViewValue('A');
			scope.$digest();
			expect(scope.form.$valid).toBe(true);
			expect(_.has(modelCtrl.$error, 'hexstring')).toBe(false);
		});

		it("ok: FFFFFFFF", () => {
			modelCtrl.$setViewValue('FFFFFFFF');
			scope.$digest();
			expect(scope.form.$valid).toBe(true);
			expect(_.has(modelCtrl.$error, 'hexstring')).toBe(false);
		});

		it("bad: x", () => {
			modelCtrl.$setViewValue('x');
			scope.$digest();
			expect(scope.form.$valid).toBe(false);
			expect(_.has(modelCtrl.$error, 'hexstring')).toBe(true);
		});

		it("bad: +1", () => {
			modelCtrl.$setViewValue('+1');
			scope.$digest();
			expect(scope.form.$valid).toBe(false);
			expect(_.has(modelCtrl.$error, 'hexstring')).toBe(true);
		});

		it("bad: -1", () => {
			modelCtrl.$setViewValue('-1');
			scope.$digest();
			expect(scope.form.$valid).toBe(false);
			expect(_.has(modelCtrl.$error, 'hexstring')).toBe(true);
		});

		it("bad: 0x01", () => {
			modelCtrl.$setViewValue('0x01');
			scope.$digest();
			expect(scope.form.$valid).toBe(false);
			expect(_.has(modelCtrl.$error, 'hexstring')).toBe(true);
		});

		it("bad: 0.0", () => {
			modelCtrl.$setViewValue('0.0');
			scope.$digest();
			expect(scope.form.$valid).toBe(false);
			expect(_.has(modelCtrl.$error, 'hexstring')).toBe(true);
		});
	});
});
