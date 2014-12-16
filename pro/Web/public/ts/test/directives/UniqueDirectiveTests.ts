/// <reference path="../reference.ts" />

'use strict';

interface ITestUniqueScope extends ng.IScope {
	model: string;
	model1: string;
	model2: string;
	model3: string;
	values: string[];
	form: ng.IFormController;
}

describe("Peach", () => {
	beforeEach(module('Peach'));

	describe('UniqueDirective', () => {
		var $compile: ng.ICompileService;
		var scope: ITestUniqueScope;
		var element: ng.IAugmentedJQuery;
		var modelCtrl: ng.INgModelController;

		beforeEach(inject(($injector: ng.auto.IInjectorService) => {
			$compile = $injector.get('$compile');
			var $rootScope = $injector.get('$rootScope');
			scope = <ITestUniqueScope> $rootScope.$new();
			scope.values = ['duplicate'];
		}));

		describe('without default', () => {
			beforeEach(() => {
				var html = pithy.form({ name: 'form' }, [
					pithy.input({
						type: 'text',
						name: 'input',
						'ng-model': 'model',
						'peach:unique': 'values'
					})
				]).toString();

				element = $compile(html)(scope);
				modelCtrl = <ng.INgModelController> scope.form['input'];
			});

			it("validates unique values", () => {
				modelCtrl.$setViewValue('unique');
				scope.$digest();
				expect(scope.model).toEqual('unique');
				expect(modelCtrl.$valid).toBe(true);
			});

			it("invalidates duplicate values", () => {
				modelCtrl.$setViewValue('duplicate');
				scope.$digest();
				expect(scope.model).toEqual('duplicate');
				expect(modelCtrl.$valid).toBe(false);
			});
		});

		describe('with default', () => {
			beforeEach(() => {
				var html = pithy.form({ name: 'form' }, [
					pithy.input({
						type: 'text',
						name: 'input',
						'ng-model': 'model',
						'peach:unique': 'values',
						'peach:unique-default': 'duplicate'
					})
				]).toString();

				element = $compile(html)(scope);
				modelCtrl = <ng.INgModelController> scope.form['input'];
			});

			it("validates unique values", () => {
				modelCtrl.$setViewValue('unique');
				scope.$digest();
				expect(scope.model).toEqual('unique');
				expect(modelCtrl.$valid).toBe(true);
			});

			it("invalidates duplicate values", () => {
				modelCtrl.$setViewValue('duplicate');
				scope.$digest();
				expect(scope.model).toEqual('duplicate');
				expect(modelCtrl.$valid).toBe(false);
			});

			it("uses default value for comparison", () => {
				modelCtrl.$setViewValue('');
				scope.$digest();
				expect(scope.model).toEqual('');
				expect(modelCtrl.$valid).toBe(false);
			});
		});
	});

	describe('UniqueChannelDirective', () => {
		var $compile: ng.ICompileService;
		var scope: ITestUniqueScope;
		var element: ng.IAugmentedJQuery;
		var modelCtrl1: ng.INgModelController;
		var modelCtrl2: ng.INgModelController;
		var modelCtrl3: ng.INgModelController;

		describe('without default', () => {
			beforeEach(inject(($injector: ng.auto.IInjectorService) => {
				$compile = $injector.get('$compile');
				var $rootScope = $injector.get('$rootScope');
				scope = <ITestUniqueScope> $rootScope.$new();

				var html = pithy.form({ name: 'form' }, [
					pithy.input({
						type: 'text',
						name: 'input1',
						'ng-model': 'model1',
						'peach:unique-channel': 'channel'
					}),
					pithy.input({
						type: 'text',
						name: 'input2',
						'ng-model': 'model2',
						'peach:unique-channel': 'channel'
					}),
					pithy.input({
						type: 'text',
						name: 'input3',
						'ng-model': 'model3',
						'peach:unique-channel': 'channel'
					})
				]).toString();

				element = $compile(html)(scope);
				modelCtrl1 = <ng.INgModelController> scope.form['input1'];
				modelCtrl2 = <ng.INgModelController> scope.form['input2'];
				modelCtrl3 = <ng.INgModelController> scope.form['input3'];
			}));

			it("validates unique values", () => {
				modelCtrl1.$setViewValue('unique1');
				modelCtrl2.$setViewValue('unique2');
				modelCtrl3.$setViewValue('unique3');
				scope.$digest();
				expect(scope.model1).toEqual('unique1');
				expect(scope.model2).toEqual('unique2');
				expect(scope.model3).toEqual('unique3');
				expect(modelCtrl1.$valid).toBe(true);
				expect(modelCtrl2.$valid).toBe(true);
				expect(modelCtrl3.$valid).toBe(true);
			});

			it("invalidates duplicate values", () => {
				modelCtrl1.$setViewValue('duplicate');
				modelCtrl2.$setViewValue('duplicate');
				modelCtrl3.$setViewValue('unique');
				scope.$digest();
				expect(scope.model1).toEqual('duplicate');
				expect(scope.model2).toEqual('duplicate');
				expect(scope.model3).toEqual('unique');
				expect(modelCtrl1.$valid).toBe(false);
				expect(modelCtrl2.$valid).toBe(false);
				expect(modelCtrl3.$valid).toBe(true);

				modelCtrl1.$setViewValue('unique');
				scope.$digest();
				expect(scope.model1).toEqual('unique');
				expect(scope.model2).toEqual('duplicate');
				expect(scope.model3).toEqual('unique');
				expect(modelCtrl1.$valid).toBe(false);
				expect(modelCtrl2.$valid).toBe(true);
				expect(modelCtrl3.$valid).toBe(false);

				modelCtrl1.$setViewValue('unique1');
				scope.$digest();
				expect(scope.model1).toEqual('unique1');
				expect(scope.model2).toEqual('duplicate');
				expect(scope.model3).toEqual('unique');
				expect(modelCtrl1.$valid).toBe(true);
				expect(modelCtrl2.$valid).toBe(true);
				expect(modelCtrl3.$valid).toBe(true);
			});
		});

		describe('with default', () => {
			beforeEach(inject(($injector: ng.auto.IInjectorService) => {
				$compile = $injector.get('$compile');
				var $rootScope = $injector.get('$rootScope');
				scope = <ITestUniqueScope> $rootScope.$new();

				var html = pithy.form({ name: 'form' }, [
					pithy.input({
						type: 'text',
						name: 'input1',
						'ng-model': 'model1',
						'peach:unique-channel': 'channel',
						'peach:unique-default': 'default1'
					}),
					pithy.input({
						type: 'text',
						name: 'input2',
						'ng-model': 'model2',
						'peach:unique-channel': 'channel',
						'peach:unique-default': 'default2'
					}),
					pithy.input({
						type: 'text',
						name: 'input3',
						'ng-model': 'model3',
						'peach:unique-channel': 'channel',
						'peach:unique-default': 'default3'
					})
				]).toString();

				element = $compile(html)(scope);
				modelCtrl1 = <ng.INgModelController> scope.form['input1'];
				modelCtrl2 = <ng.INgModelController> scope.form['input2'];
				modelCtrl3 = <ng.INgModelController> scope.form['input3'];
			}));

			it("validates unique values", () => {
				modelCtrl1.$setViewValue('unique1');
				modelCtrl2.$setViewValue('unique2');
				modelCtrl3.$setViewValue('unique3');
				scope.$digest();
				expect(scope.model1).toEqual('unique1');
				expect(scope.model2).toEqual('unique2');
				expect(scope.model3).toEqual('unique3');
				expect(modelCtrl1.$valid).toBe(true);
				expect(modelCtrl2.$valid).toBe(true);
				expect(modelCtrl3.$valid).toBe(true);
			});

			it("invalidates duplicate values", () => {
				modelCtrl1.$setViewValue('duplicate');
				modelCtrl2.$setViewValue('duplicate');
				modelCtrl3.$setViewValue('unique');
				scope.$digest();
				expect(scope.model1).toEqual('duplicate');
				expect(scope.model2).toEqual('duplicate');
				expect(scope.model3).toEqual('unique');
				expect(modelCtrl1.$valid).toBe(false);
				expect(modelCtrl2.$valid).toBe(false);
				expect(modelCtrl3.$valid).toBe(true);

				modelCtrl1.$setViewValue('unique');
				scope.$digest();
				expect(scope.model1).toEqual('unique');
				expect(scope.model2).toEqual('duplicate');
				expect(scope.model3).toEqual('unique');
				expect(modelCtrl1.$valid).toBe(false);
				expect(modelCtrl2.$valid).toBe(true);
				expect(modelCtrl3.$valid).toBe(false);

				modelCtrl1.$setViewValue('unique1');
				scope.$digest();
				expect(scope.model1).toEqual('unique1');
				expect(scope.model2).toEqual('duplicate');
				expect(scope.model3).toEqual('unique');
				expect(modelCtrl1.$valid).toBe(true);
				expect(modelCtrl2.$valid).toBe(true);
				expect(modelCtrl3.$valid).toBe(true);
			});

			it("uses default value for comparison", () => {
				modelCtrl1.$setViewValue('');
				modelCtrl2.$setViewValue('value2');
				modelCtrl3.$setViewValue('value3');
				scope.$digest();
				expect(scope.model1).toEqual('');
				expect(scope.model2).toEqual('value2');
				expect(scope.model3).toEqual('value3');
				expect(modelCtrl1.$valid).toBe(true);
				expect(modelCtrl2.$valid).toBe(true);
				expect(modelCtrl3.$valid).toBe(true);

				modelCtrl1.$setViewValue('');
				modelCtrl2.$setViewValue('default1');
				modelCtrl3.$setViewValue('value3');
				scope.$digest();
				expect(scope.model1).toEqual('');
				expect(scope.model2).toEqual('default1');
				expect(scope.model3).toEqual('value3');
				expect(modelCtrl1.$valid).toBe(false);
				expect(modelCtrl2.$valid).toBe(false);
				expect(modelCtrl3.$valid).toBe(true);

				modelCtrl1.$setViewValue('');
				modelCtrl2.$setViewValue('default1');
				modelCtrl3.$setViewValue('default1');
				scope.$digest();
				expect(scope.model1).toEqual('');
				expect(scope.model2).toEqual('default1');
				expect(scope.model3).toEqual('default1');
				expect(modelCtrl1.$valid).toBe(false);
				expect(modelCtrl2.$valid).toBe(false);
				expect(modelCtrl3.$valid).toBe(false);
			});

			it("each element can specify a different default value", () => {
				modelCtrl1.$setViewValue('');
				modelCtrl2.$setViewValue('');
				modelCtrl3.$setViewValue('value3');
				scope.$digest();
				expect(scope.model1).toEqual('');
				expect(scope.model2).toEqual('');
				expect(scope.model3).toEqual('value3');
				expect(modelCtrl1.$valid).toBe(true);
				expect(modelCtrl2.$valid).toBe(true);
				expect(modelCtrl3.$valid).toBe(true);
			});
		});

		describe('with ignore & default', () => {
			beforeEach(inject(($injector: ng.auto.IInjectorService) => {
				$compile = $injector.get('$compile');
				var $rootScope = $injector.get('$rootScope');
				scope = <ITestUniqueScope> $rootScope.$new();

				var html = pithy.form({ name: 'form' }, [
					pithy.input({
						type: 'text',
						name: 'input1',
						'ng-model': 'model1',
						'peach:unique-channel': 'channel',
						'peach:unique-ignore': '^starts'
					}),
					pithy.input({
						type: 'text',
						name: 'input2',
						'ng-model': 'model2',
						'peach:unique-channel': 'channel',
						'peach:unique-default': 'default2',
						'peach:unique-ignore': 'default2'
					})
				]).toString();

				element = $compile(html)(scope);
				modelCtrl1 = <ng.INgModelController> scope.form['input1'];
				modelCtrl2 = <ng.INgModelController> scope.form['input2'];
			}));

			it("validates unique values", () => {
				modelCtrl1.$setViewValue('unique1');
				modelCtrl2.$setViewValue('unique2');
				scope.$digest();
				expect(scope.model1).toEqual('unique1');
				expect(scope.model2).toEqual('unique2');
				expect(modelCtrl1.$valid).toBe(true);
				expect(modelCtrl2.$valid).toBe(true);
			});

			it("invalidates duplicate values", () => {
				modelCtrl1.$setViewValue('duplicate');
				modelCtrl2.$setViewValue('duplicate');
				scope.$digest();
				expect(scope.model1).toEqual('duplicate');
				expect(scope.model2).toEqual('duplicate');
				expect(modelCtrl1.$valid).toBe(false);
				expect(modelCtrl2.$valid).toBe(false);

				modelCtrl1.$setViewValue('unique');
				scope.$digest();
				expect(scope.model1).toEqual('unique');
				expect(scope.model2).toEqual('duplicate');
				expect(modelCtrl1.$valid).toBe(true);
				expect(modelCtrl2.$valid).toBe(true);
			});

			it("honors ignore", () => {
				modelCtrl1.$setViewValue('starts-12345');
				modelCtrl2.$setViewValue('starts-12345');
				scope.$digest();
				expect(scope.model1).toEqual('starts-12345');
				expect(scope.model2).toEqual('starts-12345');
				expect(modelCtrl1.$valid).toBe(true);
				expect(modelCtrl2.$valid).toBe(false);
			});

			it("honors ignore & default", () => {
				modelCtrl1.$setViewValue('default2');
				modelCtrl2.$setViewValue('');
				scope.$digest();
				expect(scope.model1).toEqual('default2');
				expect(scope.model2).toEqual('');
				expect(modelCtrl1.$valid).toBe(false);
				expect(modelCtrl2.$valid).toBe(true);
			});
		});
	});
});
