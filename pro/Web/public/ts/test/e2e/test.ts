/// <reference path="reference.ts" />

'use strict';

describe('Peach dashboard', () => {
	
	beforeEach(() => {
		browser.get('/');
	});

	it('should have a title', () => {
		expect(browser.getTitle()).toBe('Peach Fuzzer');
	});

	function getTreeNode(tree: protractor.ElementFinder, byText) {
		var nodes = tree.all(by.css('li div'));
		return nodes.filter((element, index) => {
			return element.getText().then(text => {
				return text === byText;
			});
		}).first();
	}

	it('should continue to dashboard once a Pit is selected', () => {
		var selectButton = $('.navbar .nav li');
		expect(selectButton.getText()).toBe('Select a Pit');

		var modal = $('.modal-dialog');
		expect(modal.isPresent()).toBe(true);

		var submit = modal.$('button[type=submit]');
		expect(submit.isEnabled()).toBe(false);

		var tree = $('div[treecontrol]');
		expect(tree.isPresent()).toBe(true);

		getTreeNode(tree, 'User Library').click();
		expect(submit.isEnabled()).toBe(false);

		getTreeNode(tree, 'Test').click();
		expect(submit.isEnabled()).toBe(false);

		getTreeNode(tree, 'randofaulter').click();
		expect(submit.isEnabled()).toBe(true);

		submit.click();
		expect(modal.isPresent()).toBe(false);

		expect(selectButton.getText()).toBe('randofaulter');
	});
});
