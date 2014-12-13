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

describe('isEmpty', () => {
	it("0         -> !isEmpty", () => expect(Peach.isEmpty(0)).toBe(false));
	it("{}        -> !isEmpty", () => expect(Peach.isEmpty({})).toBe(false));
	it("[]        -> !isEmpty", () => expect(Peach.isEmpty([])).toBe(false));
	it("''        -> isEmpty", () => expect(Peach.isEmpty('')).toBe(true));
	it("null      -> isEmpty", () => expect(Peach.isEmpty(null)).toBe(true));
	it("undefined -> isEmpty", () => expect(Peach.isEmpty(undefined)).toBe(true));
	it("{} (_)    -> isEmpty", () => expect(_.isEmpty({})).toBe(true));
});

describe("Peach", () => {
	beforeEach(module('Peach'));

	describe('PitService', () => {
		var $httpBackend: ng.IHttpBackendService;
		var $modal: ng.ui.bootstrap.IModalService;
		var service: Peach.Services.PitService;
		var spyOpen: jasmine.Spy;

		beforeEach(inject(($injector: ng.auto.IInjectorService) => {
			$modal = $injector.get('$modal');
			spyOpen = spyOn($modal, 'open');

			$httpBackend = $injector.get('$httpBackend');
			$httpBackend.expectGET('/p/libraries').respond([
				{ libraryUrl: '/p/libraries/LIB_GUID', locked: false }
			]);
			service = $injector.get('PitService');
			$httpBackend.flush();
		}));

		afterEach(() => {
			$httpBackend.verifyNoOutstandingExpectation();
			$httpBackend.verifyNoOutstandingRequest();
		});

		it("new", () => {
			expect(_.isObject(service)).toBe(true);
		});

		describe('when a Pit is not selected', () => {
			it("get Name is (none)", () => {
				expect(service.Name).toBe('(none)');
			});

			it("IsConfigured is false", () => {
				expect(service.IsConfigured).toBe(false);
			});
		});

		describe('when a Pit is selected', () => {
			var pitUrl = "/p/pits/PIT_GUID";
			var pit;

			describe('which is not configured', () => {
				beforeEach(() => {
					pit = {
						name: 'My Pit',
						versions: [{ configured: false }]
					};
					$httpBackend.expectGET(pitUrl).respond(pit);
					service.SelectPit(pitUrl);
					$httpBackend.flush();
				});

				it("get Name is valid", () => {
					expect(service.Name).toBe(pit.name);
				});

				it("get IsConfigured is false", () => {
					expect(service.IsConfigured).toBe(false);
				});

				it('has one version', () => {
					expect(service.Pit.versions.length).toBe(1);
				});
			});

			describe('which is already configured', () => {
				beforeEach(() => {
					pit = {
						name: 'My Pit',
						versions: [{ configured: true }]
					};
					$httpBackend.expectGET(pitUrl).respond(pit);
					service.SelectPit(pitUrl);
					$httpBackend.flush();
				});

				it("get Name is valid", () => {
					expect(service.Name).toBe(pit.name);
				});

				it("get IsConfigured is true", () => {
					expect(service.IsConfigured).toBe(true);
				});

				it('has one version', () => {
					expect(service.Pit.versions.length).toBe(1);
				});
			});

			describe('which is locked', () => {
				describe('CopyPit is successful', () => {
					var copy;
					beforeEach(() => {
						pit = {
							name: 'My Pit',
							locked: true
						};
						copy = {
							name: 'Copied Pit',
							locked: false
						};

						// fake out the CopyPitController
						spyOpen.and.returnValue({
							result: {
								then: (callback) => {
									var promise = service.CopyPit(copy);
									promise.then((x) => {
										callback(x);
									});
								},
								catch: () => {}
							}
						});

						$httpBackend.expectGET(pitUrl).respond(pit);
						$httpBackend.expectPOST("/p/pits").respond(copy);
						service.SelectPit(pitUrl);
						$httpBackend.flush();
					});

					it("get Name is valid", () => {
						expect(service.Name).toBe(copy.name);
					});

					it("Pit is not selected", () => {
						expect(_.isObject(service.Pit)).toBe(true);
					});
				});

				describe('CopyPit is cancelled', () => {
					beforeEach(() => {
						pit = {
							name: 'My Pit',
							locked: true
						};

						// fake out the CopyPitController
						spyOpen.and.returnValue({
							result: {
								then: () => { },
								catch: (callback) => {
									callback();
								}
							}
						});

						$httpBackend.expectGET(pitUrl).respond(pit);
						service.SelectPit(pitUrl);
						$httpBackend.flush();
					});

					it("get Name is valid", () => {
						expect(service.Name).toBe('(none)');
					});

					it("Pit is not selected", () => {
						expect(service.Pit).toBeUndefined();
					});
				});
			});
		});
	});
});
