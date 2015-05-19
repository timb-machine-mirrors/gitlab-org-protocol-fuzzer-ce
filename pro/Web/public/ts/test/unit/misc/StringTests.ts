/// <reference path="../reference.ts" />

'use strict';

describe('startsWith', () => {
	it("'abc' startsWith 'a' is true", () => {
		expect('abc'.startsWith('a')).toBe(true);
	});
	it("'abc' startsWith 'ab' is true", () => {
		expect('abc'.startsWith('ab')).toBe(true);
	});
	it("'abc' startsWith 'abc' is true", () => {
		expect('abc'.startsWith('abc')).toBe(true);
	});
	it("'abc' startsWith 'abcd' is false", () => {
		expect('abc'.startsWith('abcd')).toBe(false);
	});
	it("'abc' startsWith 'b' is false", () => {
		expect('abc'.startsWith('b')).toBe(false);
	});
	it("'abc' startsWith '' is true", () => {
		expect('abc'.startsWith('')).toBe(true);
	});
});
