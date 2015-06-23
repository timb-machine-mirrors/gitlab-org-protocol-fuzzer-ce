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

describe('format',() => {
	it('{0}h {1}m {2}s',() => {
		expect('{0}h {1}m {2}s'.format(1, 2, 3)).toBe('1h 2m 3s');
	});

	it('prefix {0} suffix',() => {
		expect('prefix {0} suffix'.format('body')).toBe('prefix body suffix');
	});

	it('{0} {0}',() => {
		expect('{0} {0}'.format('foo')).toBe('foo foo');
	});
});

describe('paddingLeft',() => {
	it('000123',() => {
		expect('123'.paddingLeft('000000')).toBe('000123');
	});

	it('   123',() => {
		expect('123'.paddingLeft('      ')).toBe('   123');
	});
});
